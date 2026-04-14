using Faturamento.Models;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(FaturamentoDbContext db)
        {
            await db.Database.EnsureCreatedAsync();
            if (await db.Invoices.AnyAsync()) return;

            var invoices = new[]
            {
                new Invoice
                {
                    Number = 1,
                    Status = InvoiceStatus.Aberta,
                    Date = DateTime.UtcNow,
                    Items = new List<InvoiceItem>
                    {
                        new InvoiceItem { ProdutoId = 1, ProdutoCodigo = "P001", ProdutoDescricao = "Produto A", Quantidade = 2 },
                        new InvoiceItem { ProdutoId = 2, ProdutoCodigo = "P002", ProdutoDescricao = "Produto B", Quantidade = 1 }
                    },
                    Total = 100.50m
                },
                new Invoice
                {
                    Number = 2,
                    Status = InvoiceStatus.Aberta,
                    Date = DateTime.UtcNow,
                    Items = new List<InvoiceItem>
                    {
                        new InvoiceItem { ProdutoId = 3, ProdutoCodigo = "P003", ProdutoDescricao = "Produto C", Quantidade = 5 }
                    },
                    Total = 200m
                }
            };

            await db.Invoices.AddRangeAsync(invoices);
            await db.SaveChangesAsync();
        }
    }
}
