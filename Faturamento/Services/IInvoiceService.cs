using Faturamento.Models;

namespace Faturamento.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<Invoice?> GetByIdAsync(int id);
        Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default);

        /// <summary>Processa impressão: baixa estoque, fecha a nota e retorna o PDF. Null se a nota não existir ou não estiver aberta.</summary>
        Task<byte[]?> PrintAsync(int invoiceId, CancellationToken cancellationToken = default);
    }
}
