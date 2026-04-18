using System.Text.Json;

namespace EstoqueService.Services
{
    // Precisa ser tipo valor (struct) para o controller conseguir usar `HasValue`/`Value`.
    public readonly record struct IdempotencyPriorResponse(int StatusCode, string? Body);

    public interface IIdempotencyService
    {
        Task<IdempotencyPriorResponse?> TryGetAsync(
            string idempotencyKey,
            string method,
            string path,
            CancellationToken cancellationToken);

        Task SaveAsync(
            string idempotencyKey,
            string method,
            string path,
            int statusCode,
            object? body,
            string contentType,
            CancellationToken cancellationToken);
    }
}

