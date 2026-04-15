using Faturamento.Exceptions;
using Faturamento.Models;
using Faturamento.Repositories;
using System.Net.Http.Json;
using System.Text.Json;

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

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            if (invoice.Items == null || !invoice.Items.Any())
                throw new ArgumentException("A nota fiscal deve conter ao menos um item.", nameof(invoice.Items));

            if (invoice.Items.Any(i => i.Quantidade <= 0))
                throw new ArgumentException("Cada item deve ter quantidade maior que zero.", nameof(invoice.Items));

            if (invoice.Total <= 0)
                throw new ArgumentException("O total da nota deve ser maior que zero.", nameof(invoice.Total));

            invoice.Date = invoice.Date == default ? DateTime.UtcNow : invoice.Date;
            invoice.Status = InvoiceStatus.Aberta;

            var all = await _repo.GetAllAsync();
            invoice.Number = all.Any() ? all.Max(i => i.Number) + 1 : 1;

            return await _repo.AddAsync(invoice);
        }

        public async Task<bool> PrintAsync(int invoiceId, CancellationToken cancellationToken = default)
        {
            var invoice = await _repo.GetByIdAsync(invoiceId);
            if (invoice == null) return false;
            if (invoice.Status != InvoiceStatus.Aberta) return false;

            var items = invoice.Items?.ToList() ?? new List<InvoiceItem>();
            if (items.Count == 0)
                throw new InvalidOperationException("Nota sem itens não pode ser impressa.");

            var client = _httpClientFactory.CreateClient("estoque");
            var applied = new List<(string Codigo, int Quantidade)>();

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var codigoEncoded = Uri.EscapeDataString(item.ProdutoCodigo);

                HttpResponseMessage resp;
                try
                {
                    resp = await client.PostAsJsonAsync(
                        $"/api/produtos/{codigoEncoded}/saida",
                        new { Quantidade = item.Quantidade },
                        cancellationToken);
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

            return await _repo.PrintAndCloseAsync(invoiceId);
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
                        new { Quantidade = quantidade },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao reverter saldo do produto {Codigo} após erro na impressão.", codigo);
                }
            }
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
