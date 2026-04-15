using Microsoft.EntityFrameworkCore;
using EstoqueService.Models;

namespace EstoqueService.Data
{
    public class EstoqueDbContext : DbContext
    {
        public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options) { }

        public DbSet<Produto> Produtos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Produto>()
                .HasIndex(p => p.Codigo)
                .IsUnique();

            modelBuilder.Entity<Produto>()
                .UseXminAsConcurrencyToken();

            modelBuilder.Entity<Produto>()
                .Property(p => p.Descricao)
                .IsRequired();

            modelBuilder.Entity<Produto>()
                .Property(p => p.Codigo)
                .IsRequired();
        }
    }
}
