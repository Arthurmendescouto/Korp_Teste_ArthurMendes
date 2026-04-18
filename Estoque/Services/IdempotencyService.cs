using System.Collections.Concurrent;
using System.Text.Json;

namespace EstoqueService.Services
{
    public class IdempotencyService : IIdempotencyService
    {
        // Simples (em memória) para cumprir o contrato do controller.
        // Em produção, isso normalmente seria persistido (Redis/DB).
        private readonly ConcurrentDictionary<string, IdempotencyPriorResponse> _store = new();

        public Task<IdempotencyPriorResponse?> TryGetAsync(
            string idempotencyKey,
            string method,
            string path,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return Task.FromResult<IdempotencyPriorResponse?>(null);

            var key = BuildStoreKey(idempotencyKey, method, path);
            if (_store.TryGetValue(key, out var prior))
                return Task.FromResult<IdempotencyPriorResponse?>(prior);

            return Task.FromResult<IdempotencyPriorResponse?>(null);
        }

        public Task SaveAsync(
            string idempotencyKey,
            string method,
            string path,
            int statusCode,
            object? body,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return Task.CompletedTask;

            string? bodySerialized = null;
            if (body is not null && !string.IsNullOrWhiteSpace(contentType))
            {
                if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    bodySerialized = JsonSerializer.Serialize(body);
                }
                else
                {
                    bodySerialized = body.ToString();
                }
            }

            var key = BuildStoreKey(idempotencyKey, method, path);
            _store[key] = new IdempotencyPriorResponse(statusCode, bodySerialized);

            return Task.CompletedTask;
        }

        private static string BuildStoreKey(string idempotencyKey, string method, string path)
            => $"{method}:{path}:{idempotencyKey}";
    }
}

