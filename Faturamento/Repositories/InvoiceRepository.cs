using Faturamento.Data;
using Faturamento.Models;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly FaturamentoDbContext _db;

        public InvoiceRepository(FaturamentoDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await _db.Invoices
                .AsNoTracking()
                .Include(i => i.Items)
                .OrderBy(i => i.Number)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await _db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice> AddAsync(Invoice invoice)
        {
            foreach (var item in invoice.Items)
                item.ProdutoCodigo = item.ProdutoCodigo ?? string.Empty;

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return invoice;
        }

        public Task UpdateAsync(Invoice invoice)
        {
            _db.Invoices.Update(invoice);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }

        public async Task<bool> PrintAndCloseAsync(int invoiceId)
        {
            var invoice = await _db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return false;
            if (invoice.Status != Models.InvoiceStatus.Aberta) return false;

            invoice.Status = Models.InvoiceStatus.Fechada;
            _db.Invoices.Update(invoice);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
