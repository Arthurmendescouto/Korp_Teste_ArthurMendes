using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using EstoqueService.Models;
using EstoqueService.Services;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoService _service;
        private readonly ILogger<ProdutosController> _logger;
        private readonly IIdempotencyService _idempotency;

        public ProdutosController(IProdutoService service, ILogger<ProdutosController> logger, IIdempotencyService idempotency)
        {
            _service = service;
            _logger = logger;
            _idempotency = idempotency;
        }

        /// <summary>
        /// Lista todos os produtos cadastrados.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.ListarAsync();
            return Ok(list);
        }

        /// <summary>
        /// Obtém um produto pelo seu código.
        /// </summary>
        [HttpGet("{codigo}")]
        public async Task<IActionResult> GetByCodigo(string codigo)
        {
            var prod = await _service.ObterPorCodigoAsync(codigo);
            if (prod == null) return NotFound();
            return Ok(prod);
        }

        public record CreateProdutoRequest(
            string Codigo,
            string Descricao,
            int SaldoInicial = 10
        );

        /// <summary>
        /// Cria um novo produto.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProdutoRequest req,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct)
        {
            var path = HttpContext.Request.Path.Value ?? string.Empty;
            const string method = "POST";

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var prior = await _idempotency.TryGetAsync(idempotencyKey, method, path, ct);
                if (prior.HasValue)
                    return StatusCode(prior.Value.StatusCode, string.IsNullOrWhiteSpace(prior.Value.Body) ? null : JsonSerializer.Deserialize<object>(prior.Value.Body));
            }

            var produto = await _service.CriarAsync(req.Codigo, req.Descricao, req.SaldoInicial, ct);
            var result = CreatedAtAction(nameof(GetByCodigo), new { codigo = produto.Codigo }, produto);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                await _idempotency.SaveAsync(idempotencyKey, method, path, StatusCodes.Status201Created, produto, "application/json", ct);

            return result;
        }

        public record RegistrarMovimentoRequest(int Quantidade = 1);

        /// <summary>
        /// Registra uma saída (baixa) no estoque do produto.
        /// Usado pelo microsserviço de Faturamento ao imprimir uma nota fiscal.
        /// </summary>
        [HttpPost("{codigo}/saida")]
        public async Task<IActionResult> RegistrarSaida(
            string codigo,
            [FromBody] RegistrarMovimentoRequest req,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct)
        {
            var path = HttpContext.Request.Path.Value ?? string.Empty;
            const string method = "POST";

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var prior = await _idempotency.TryGetAsync(idempotencyKey, method, path, ct);
                if (prior.HasValue)
                    return StatusCode(prior.Value.StatusCode);
            }

            var ok = await _service.RegistrarSaidaAsync(codigo, req.Quantidade, ct);
            if (!ok) return BadRequest(new { message = "Saída inválida (produto não existe, quantidade inválida ou saldo insuficiente)." });

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                await _idempotency.SaveAsync(idempotencyKey, method, path, StatusCodes.Status204NoContent, null, "application/json", ct);

            return NoContent();
        }

        /// <summary>
        /// Registra uma entrada no estoque do produto (reposição).
        /// Usado pelo Faturamento para compensar (reverter) uma saída caso a impressão falhe no meio do processo.
        /// </summary>
        [HttpPost("{codigo}/entrada")]
        public async Task<IActionResult> RegistrarEntrada(
            string codigo,
            [FromBody] RegistrarMovimentoRequest req,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct)
        {
            var path = HttpContext.Request.Path.Value ?? string.Empty;
            const string method = "POST";

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var prior = await _idempotency.TryGetAsync(idempotencyKey, method, path, ct);
                if (prior.HasValue)
                    return StatusCode(prior.Value.StatusCode);
            }

            var ok = await _service.RegistrarEntradaAsync(codigo, req.Quantidade, ct);
            if (!ok) return BadRequest(new { message = "Entrada inválida (produto não existe ou quantidade inválida)." });

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                await _idempotency.SaveAsync(idempotencyKey, method, path, StatusCodes.Status204NoContent, null, "application/json", ct);

            return NoContent();
        }

        public record AtualizarSaldoRequest(int NovoSaldo = 10);

        /// <summary>
        /// Atualiza o saldo do produto para um valor final (não é incremento).
        /// Útil para correções administrativas.
        /// </summary>
        [HttpPut("{codigo}/saldo")]
        public async Task<IActionResult> AtualizarSaldo(
            string codigo,
            [FromBody] AtualizarSaldoRequest req,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct)
        {
            var path = HttpContext.Request.Path.Value ?? string.Empty;
            const string method = "PUT";

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var prior = await _idempotency.TryGetAsync(idempotencyKey, method, path, ct);
                if (prior.HasValue)
                    return StatusCode(prior.Value.StatusCode);
            }

            var ok = await _service.AtualizarSaldoAsync(codigo, req.NovoSaldo, ct);
            if (!ok) return BadRequest(new { message = "Atualização inválida (produto não existe ou novo saldo inválido)." });

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                await _idempotency.SaveAsync(idempotencyKey, method, path, StatusCodes.Status204NoContent, null, "application/json", ct);

            return NoContent();
        }
    }
}
