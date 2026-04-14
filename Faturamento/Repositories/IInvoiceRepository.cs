using Faturamento.Models;

namespace Faturamento.Repositories
{
    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<Invoice?> GetByIdAsync(int id);
        Task<Invoice> AddAsync(Invoice invoice);
        Task<bool> PrintAndCloseAsync(int invoiceId);
        Task UpdateAsync(Invoice invoice);
        Task SaveChangesAsync();
    }
}
