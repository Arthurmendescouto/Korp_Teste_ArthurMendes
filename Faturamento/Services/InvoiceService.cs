using System.Net.Http.Json;
using System.Text.Json;
using Faturamento.Exceptions;
using Faturamento.Models;
using Faturamento.Repositories;

namespace Faturamento.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _repo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(
            IInvoiceRepository repo,
            IHttpClientFactory httpClientFactory,
            ILogger<InvoiceService> logger)
        {
            _repo = repo;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<Invoice?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

        public async Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            if (invoice.Items == null || !invoice.Items.Any())
                throw new ArgumentException("A nota fiscal deve conter ao menos um item.", nameof(invoice.Items));

            if (invoice.Items.Any(i => i.Quantidade <= 0))
                throw new ArgumentException("Cada item deve ter quantidade maior que zero.", nameof(invoice.Items));

            if (invoice.Total <= 0)
                throw new ArgumentException("O total da nota deve ser maior que zero.", nameof(invoice.Total));

            var client = _httpClientFactory.CreateClient("estoque");
            foreach (var item in invoice.Items)
            {
                var codigo = item.ProdutoCodigo?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(codigo))
                    throw new ArgumentException("Cada item deve informar o código do produto.", nameof(invoice.Items));

                var encoded = Uri.EscapeDataString(codigo);
                HttpResponseMessage resp;
                try
                {
                    resp = await client.GetAsync($"/api/produtos/{encoded}", cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Falha de rede ao consultar o estoque para o produto {Codigo}.", codigo);
                    throw new EstoqueUnavailableException(
                        "Não foi possível contatar o serviço de estoque para validar os produtos da nota.",
                        ex);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Timeout ao consultar o estoque para o produto {Codigo}.", codigo);
                    throw new EstoqueUnavailableException(
                        "O serviço de estoque não respondeu a tempo ao validar os produtos da nota.",
                        ex);
                }

                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException(
                        $"Produto com código '{codigo}' não foi encontrado no estoque. Cadastre o produto antes de emitir a nota.");
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var detail = await LerDetalheErroAsync(resp, cancellationToken);
                    throw new EstoqueUnavailableException(
                        $"O serviço de estoque retornou {(int)resp.StatusCode} ao validar o produto '{codigo}'. {detail}".Trim());
                }

                var (id, descricao) = await LerProdutoResumoAsync(resp, cancellationToken);
                item.ProdutoId = id;
                item.ProdutoDescricao = descricao;
            }

            invoice.Date = invoice.Date == default ? DateTime.UtcNow : invoice.Date;
            invoice.Status = InvoiceStatus.Aberta;

            var all = await _repo.GetAllAsync();
            invoice.Number = all.Any() ? all.Max(i => i.Number) + 1 : 1;

            return await _repo.AddAsync(invoice);
        }

        public async Task<byte[]?> PrintAsync(int invoiceId, CancellationToken cancellationToken = default)
        {
            var invoice = await _repo.GetByIdAsync(invoiceId);
            if (invoice == null) return null;
            if (invoice.Status != InvoiceStatus.Aberta) return null;

            var items = invoice.Items?.ToList() ?? new List<InvoiceItem>();
            if (items.Count == 0)
                throw new InvalidOperationException("Nota sem itens não pode ser impressa.");

            var client = _httpClientFactory.CreateClient("estoque");
            var applied = new List<(string Codigo, int Quantidade)>();

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var codigoEncoded = Uri.EscapeDataString(item.ProdutoCodigo);
                var idempotencyKey = $"nf-print-{invoiceId}-line-{index}";

                HttpResponseMessage resp;
                try
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Post,
                        $"/api/produtos/{codigoEncoded}/saida")
                    {
                        Content = JsonContent.Create(new { quantidade = item.Quantidade })
                    };
                    request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
                    resp = await client.SendAsync(request, cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Falha de rede ao chamar o serviço de estoque (item {Codigo}).", item.ProdutoCodigo);
                    await CompensarAsync(client, applied, cancellationToken);
                    throw new EstoqueUnavailableException(
                        "Não foi possível contatar o serviço de estoque. Tente novamente em instantes.",
                        ex);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Timeout ao chamar o serviço de estoque (item {Codigo}).", item.ProdutoCodigo);
                    await CompensarAsync(client, applied, cancellationToken);
                    throw new EstoqueUnavailableException(
                        "O serviço de estoque não respondeu a tempo. Nenhuma alteração de saldo foi concluída para esta operação.",
                        ex);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var detail = await LerDetalheErroAsync(resp, cancellationToken);
                    await CompensarAsync(client, applied, cancellationToken);

                    if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new InvalidOperationException(
                            string.IsNullOrWhiteSpace(detail)
                                ? $"Estoque recusou o ajuste para o produto {item.ProdutoCodigo} (saldo insuficiente ou produto inexistente)."
                                : detail);
                    }

                    throw new EstoqueUnavailableException(
                        $"O serviço de estoque retornou {(int)resp.StatusCode}. {detail}".Trim());
                }

                applied.Add((item.ProdutoCodigo, item.Quantidade));
            }

            var closed = await _repo.PrintAndCloseAsync(invoiceId);
            if (!closed) return null;

            var final = await _repo.GetByIdAsync(invoiceId);
            if (final == null) return null;

            return InvoicePdfGenerator.Generate(final);
        }

        private async Task CompensarAsync(
            HttpClient client,
            List<(string Codigo, int Quantidade)> applied,
            CancellationToken cancellationToken)
        {
            for (var i = applied.Count - 1; i >= 0; i--)
            {
                var (codigo, quantidade) = applied[i];
                try
                {
                    var encoded = Uri.EscapeDataString(codigo);
                    await client.PostAsJsonAsync(
                        $"/api/produtos/{encoded}/entrada",
                        new { quantidade },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao reverter saldo do produto {Codigo} após erro na impressão.", codigo);
                }
            }
        }

        private static async Task<(int Id, string Descricao)> LerProdutoResumoAsync(
            HttpResponseMessage resp,
            CancellationToken ct)
        {
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetInt32();
            var descricao = root.TryGetProperty("descricao", out var d) ? d.GetString() ?? string.Empty : string.Empty;
            return (id, descricao);
        }

        private static async Task<string> LerDetalheErroAsync(HttpResponseMessage resp, CancellationToken ct)
        {
            try
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json)) return string.Empty;

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    return m.GetString() ?? string.Empty;
                if (doc.RootElement.TryGetProperty("error", out var e))
                    return e.GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }

            return string.Empty;
        }
    }
}
