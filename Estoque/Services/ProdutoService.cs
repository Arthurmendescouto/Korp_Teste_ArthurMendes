using EstoqueService.Models;
using EstoqueService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Services
{
    public class ProdutoService : IProdutoService
    {
        private readonly IProdutoRepository _repo;
        private const int MaxConcurrencyRetries = 3;

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
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("O código do produto é obrigatório.", nameof(codigo));
            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("A descrição do produto é obrigatória.", nameof(descricao));
            if (saldoInicial < 0)
                throw new ArgumentException("O saldo inicial não pode ser negativo.", nameof(saldoInicial));

            var exists = await _repo.GetByCodigoAsync(codigo, ct);
            if (exists != null) throw new InvalidOperationException("Produto com esse código já existe.");

            var produto = new Produto
            {
                Codigo = codigo.Trim(),
                Descricao = descricao.Trim(),
                Saldo = saldoInicial
            };

            await _repo.AddAsync(produto, ct);
            await _repo.SaveChangesAsync(ct);
            return produto;
        }

        public async Task<bool> RegistrarSaidaAsync(string codigo, int quantidade, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return false;
            if (quantidade <= 0) return false;

            for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
            {
                var produto = await _repo.GetByCodigoAsync(codigo, ct);
                if (produto == null) return false;

                var novoSaldo = produto.Saldo - quantidade;
                if (novoSaldo < 0) return false;

                produto.Saldo = novoSaldo;
                await _repo.UpdateAsync(produto, ct);

                try
                {
                    await _repo.SaveChangesAsync(ct);
                    return true;
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
                {
                }
            }

            return false;
        }

        public async Task<bool> RegistrarEntradaAsync(string codigo, int quantidade, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return false;
            if (quantidade <= 0) return false;

            for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
            {
                var produto = await _repo.GetByCodigoAsync(codigo, ct);
                if (produto == null) return false;

                produto.Saldo += quantidade;
                await _repo.UpdateAsync(produto, ct);

                try
                {
                    await _repo.SaveChangesAsync(ct);
                    return true;
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
                {
                }
            }

            return false;
        }

        public async Task<bool> AtualizarSaldoAsync(string codigo, int novoSaldo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return false;
            if (novoSaldo < 0) return false;

            for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
            {
                var produto = await _repo.GetByCodigoAsync(codigo, ct);
                if (produto == null) return false;

                produto.Saldo = novoSaldo;
                await _repo.UpdateAsync(produto, ct);

                try
                {
                    await _repo.SaveChangesAsync(ct);
                    return true;
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
                {
                }
            }

            return false;
        }
    }
}
