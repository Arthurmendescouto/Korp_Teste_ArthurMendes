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

        public record CreateProdutoRequest(
            string Codigo,
            string Descricao,
            int SaldoInicial = 10
        );

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

        public record RegistrarMovimentoRequest(int Quantidade = 1);

        /// <summary>
        /// Registra uma saída (baixa) no estoque do produto.
        /// Usado pelo microsserviço de Faturamento ao imprimir uma nota fiscal.
        /// </summary>
        [HttpPost("{codigo}/saida")]
        public async Task<IActionResult> RegistrarSaida(string codigo, [FromBody] RegistrarMovimentoRequest req, CancellationToken ct)
        {
            var ok = await _service.RegistrarSaidaAsync(codigo, req.Quantidade, ct);
            if (!ok) return BadRequest(new { message = "Saída inválida (produto não existe, quantidade inválida ou saldo insuficiente)." });
            return NoContent();
        }

        /// <summary>
        /// Registra uma entrada no estoque do produto (reposição).
        /// Usado pelo Faturamento para compensar (reverter) uma saída caso a impressão falhe no meio do processo.
        /// </summary>
        [HttpPost("{codigo}/entrada")]
        public async Task<IActionResult> RegistrarEntrada(string codigo, [FromBody] RegistrarMovimentoRequest req, CancellationToken ct)
        {
            var ok = await _service.RegistrarEntradaAsync(codigo, req.Quantidade, ct);
            if (!ok) return BadRequest(new { message = "Entrada inválida (produto não existe ou quantidade inválida)." });
            return NoContent();
        }

        public record AtualizarSaldoRequest(int NovoSaldo = 10);

        /// <summary>
        /// Atualiza o saldo do produto para um valor final (não é incremento).
        /// Útil para correções administrativas.
        /// </summary>
        [HttpPut("{codigo}/saldo")]
        public async Task<IActionResult> AtualizarSaldo(string codigo, [FromBody] AtualizarSaldoRequest req, CancellationToken ct)
        {
            var ok = await _service.AtualizarSaldoAsync(codigo, req.NovoSaldo, ct);
            if (!ok) return BadRequest(new { message = "Atualização inválida (produto não existe ou novo saldo inválido)." });
            return NoContent();
        }
    }
}
