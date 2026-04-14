using EstoqueService.Models;

namespace EstoqueService.Services
{
    public interface IProdutoService
    {
        Task<List<Produto>> ListarAsync(CancellationToken ct = default);
        Task<Produto?> ObterPorCodigoAsync(string codigo, CancellationToken ct = default);
        Task<Produto> CriarAsync(string codigo, string descricao, int saldoInicial, CancellationToken ct = default);
        Task<bool> RegistrarSaidaAsync(string codigo, int quantidade, CancellationToken ct = default);
        Task<bool> RegistrarEntradaAsync(string codigo, int quantidade, CancellationToken ct = default);
        Task<bool> AtualizarSaldoAsync(string codigo, int novoSaldo, CancellationToken ct = default);
    }
}
