using EstoqueService.Models;
using EstoqueService.Repositories;

namespace EstoqueService.Services
{
    public class ProdutoService : IProdutoService
    {
        private readonly IProdutoRepository _repo;

        public ProdutoService(IProdutoRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Produto>> ListarAsync(CancellationToken ct = default) =>
            _repo.GetAllAsync(ct);

        public Task<Produto?> ObterPorCodigoAsync(string codigo, CancellationToken ct = default) =>
            _repo.GetByCodigoAsync(codigo, ct);

        public async Task<Produto> CriarAsync(string codigo, string descricao, int saldoInicial, CancellationToken ct = default)
        {
            var exists = await _repo.GetByCodigoAsync(codigo, ct);
            if (exists != null) throw new InvalidOperationException("Produto com esse código já existe.");

            var produto = new Produto
            {
                Codigo = codigo,
                Descricao = descricao,
                Saldo = saldoInicial
            };

            await _repo.AddAsync(produto, ct);
            await _repo.SaveChangesAsync(ct);
            return produto;
        }

        public async Task<bool> RegistrarSaidaAsync(string codigo, int quantidade, CancellationToken ct = default)
        {
            if (quantidade <= 0) return false;

            var produto = await _repo.GetByCodigoAsync(codigo, ct);
            if (produto == null) return false;

            var novoSaldo = produto.Saldo - quantidade;
            if (novoSaldo < 0) return false;

            produto.Saldo = novoSaldo;
            await _repo.UpdateAsync(produto, ct);
            await _repo.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RegistrarEntradaAsync(string codigo, int quantidade, CancellationToken ct = default)
        {
            if (quantidade <= 0) return false;

            var produto = await _repo.GetByCodigoAsync(codigo, ct);
            if (produto == null) return false;

            produto.Saldo += quantidade;
            await _repo.UpdateAsync(produto, ct);
            await _repo.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> AtualizarSaldoAsync(string codigo, int novoSaldo, CancellationToken ct = default)
        {
            if (novoSaldo < 0) return false;

            var produto = await _repo.GetByCodigoAsync(codigo, ct);
            if (produto == null) return false;

            produto.Saldo = novoSaldo;
            await _repo.UpdateAsync(produto, ct);
            await _repo.SaveChangesAsync(ct);
            return true;
        }
    }
}
