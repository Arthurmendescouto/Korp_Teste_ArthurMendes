using Microsoft.AspNetCore.Mvc;
using Faturamento.Services;
using Faturamento.Models;
using Faturamento.Exceptions;

namespace Faturamento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _service;

        public InvoicesController(IInvoiceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var inv = await _service.GetByIdAsync(id);
            if (inv == null) return NotFound();
            return Ok(inv);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Invoice invoice, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateAsync(invoice);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/print")]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            try
            {
                var ok = await _service.PrintAsync(id, cancellationToken);
                if (!ok)
                    return BadRequest(new { error = "Impressão inválida: nota não existe ou não está com status Aberta." });
                return Ok(new { message = "Nota impressa com sucesso. Status atualizado para Fechada e saldos do estoque atualizados." });
            }
            catch (EstoqueUnavailableException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "Serviço de estoque indisponível ou instável.",
                    detail = ex.Message,
                    hint = "Verifique se o microsserviço Estoque está em execução e tente novamente."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
