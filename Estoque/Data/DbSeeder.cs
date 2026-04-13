using Microsoft.EntityFrameworkCore;
using EstoqueService.Models;

namespace EstoqueService.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(EstoqueDbContext db)
        {
            // Cria o schema conforme o modelo (útil para testes rápidos; em produção use migrations)
            await db.Database.EnsureCreatedAsync();

            // Se já existem produtos, assume que já está semeado
            if (await db.Produtos.AnyAsync()) return;

            var produtos = new[]
            {
                new Produto { Codigo = "P001", Descricao = "Caneta Azul", Saldo = 10 },
                new Produto { Codigo = "P002", Descricao = "Caderno A4", Saldo = 5 },
                new Produto { Codigo = "P003", Descricao = "Lapis HB", Saldo = 20 }
            };

            await db.Produtos.AddRangeAsync(produtos);
            await db.SaveChangesAsync();
        }
    }
}
