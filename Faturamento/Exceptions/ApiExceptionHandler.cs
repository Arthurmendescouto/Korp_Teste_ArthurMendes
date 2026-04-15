using Faturamento.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.Exceptions;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida."),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Operação inválida."),
            EstoqueUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Serviço de estoque indisponível."),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado.")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}

