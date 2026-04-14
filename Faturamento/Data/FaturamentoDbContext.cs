using Microsoft.EntityFrameworkCore;
using Faturamento.Models;

namespace Faturamento.Data
{
    public class FaturamentoDbContext : DbContext
    {
        public FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Items)
                .WithOne()
                .HasForeignKey(it => it.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
