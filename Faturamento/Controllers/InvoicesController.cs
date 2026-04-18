using Microsoft.AspNetCore.Mvc;
using Faturamento.Services;
using Faturamento.Models;

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

        /// <summary>
        /// Lista todas as notas fiscais.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        /// <summary>
        /// Obtém uma nota fiscal pelo identificador.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var inv = await _service.GetByIdAsync(id);
            if (inv == null) return NotFound();
            return Ok(inv);
        }

        /// <summary>
        /// Cria uma nova nota fiscal.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Invoice invoice, CancellationToken cancellationToken)
        {
            var created = await _service.CreateAsync(invoice, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Imprime (fecha) uma nota fiscal e efetua a baixa de estoque dos itens.
        /// </summary>
        [HttpPost("{id:int}/print")]
        [Produces("application/pdf")]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var pdf = await _service.PrintAsync(id, cancellationToken);
            if (pdf is null || pdf.Length == 0)
                return BadRequest(new { error = "Impressão inválida: nota não existe ou não está com status Aberta." });

            return File(pdf, "application/pdf", $"nota-{id}.pdf");
        }
    }
}
