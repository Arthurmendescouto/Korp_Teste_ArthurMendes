using Faturamento.Models;

namespace Faturamento.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<Invoice?> GetByIdAsync(int id);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<bool> PrintAsync(int invoiceId, CancellationToken cancellationToken = default);
    }
}
