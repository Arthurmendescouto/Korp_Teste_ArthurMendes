using Microsoft.EntityFrameworkCore;
using EstoqueService.Data;
using EstoqueService.Models;

namespace EstoqueService.Repositories
{
    public class ProdutoRepository : IProdutoRepository
    {
        private readonly EstoqueDbContext _db;

        public ProdutoRepository(EstoqueDbContext db)
        {
            _db = db;
        }

        public Task<List<Produto>> GetAllAsync(CancellationToken ct = default)
        {
            return _db.Produtos.AsNoTracking().ToListAsync(ct);
        }

        public Task<Produto?> GetByCodigoAsync(string codigo, CancellationToken ct = default)
        {
            return _db.Produtos.FirstOrDefaultAsync(p => p.Codigo == codigo, ct);
        }

        public async Task AddAsync(Produto produto, CancellationToken ct = default)
        {
            await _db.Produtos.AddAsync(produto, ct);
        }

        public Task UpdateAsync(Produto produto, CancellationToken ct = default)
        {
            _db.Produtos.Update(produto);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _db.SaveChangesAsync(ct);
        }
    }
}
