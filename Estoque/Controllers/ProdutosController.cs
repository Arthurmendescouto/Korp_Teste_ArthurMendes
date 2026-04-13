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

        public ProdutosController(IProdutoService service, ILogger<ProdutosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var list = await _service.ListarAsync(ct);
            return Ok(list);
        }

        [HttpGet("{codigo}")]
        public async Task<IActionResult> GetByCodigo(string codigo, CancellationToken ct)
        {
            var prod = await _service.ObterPorCodigoAsync(codigo, ct);
            if (prod == null) return NotFound();
            return Ok(prod);
        }

        public record CreateProdutoRequest(string Codigo, string Descricao, int SaldoInicial);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProdutoRequest req, CancellationToken ct)
        {
            try
            {
                var produto = await _service.CriarAsync(req.Codigo, req.Descricao, req.SaldoInicial, ct);
                return CreatedAtAction(nameof(GetByCodigo), new { codigo = produto.Codigo }, produto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        public record AjusteSaldoRequest(int Delta);

        [HttpPost("{codigo}/ajustar-saldo")]
        public async Task<IActionResult> AjustarSaldo(string codigo, [FromBody] AjusteSaldoRequest req, CancellationToken ct)
        {
            var ok = await _service.AjustarSaldoAsync(codigo, req.Delta, ct);
            if (!ok) return BadRequest(new { message = "Ajuste inválido (produto não existe ou saldo insuficiente)." });
            return NoContent();
        }
    }
}
