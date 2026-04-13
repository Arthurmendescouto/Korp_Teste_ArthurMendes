using EstoqueService.Models;

namespace EstoqueService.Repositories
{
    public interface IProdutoRepository
    {
        Task<List<Produto>> GetAllAsync(CancellationToken ct = default);
        Task<Produto?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
        Task AddAsync(Produto produto, CancellationToken ct = default);
        Task UpdateAsync(Produto produto, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
