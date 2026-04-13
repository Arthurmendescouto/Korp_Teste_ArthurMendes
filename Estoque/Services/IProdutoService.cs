using EstoqueService.Models;

namespace EstoqueService.Services
{
    public interface IProdutoService
    {
        Task<List<Produto>> ListarAsync(CancellationToken ct = default);
        Task<Produto?> ObterPorCodigoAsync(string codigo, CancellationToken ct = default);
        Task<Produto> CriarAsync(string codigo, string descricao, int saldoInicial, CancellationToken ct = default);
        Task<bool> AjustarSaldoAsync(string codigo, int delta, CancellationToken ct = default); // delta negativo para reduzir
    }
}
