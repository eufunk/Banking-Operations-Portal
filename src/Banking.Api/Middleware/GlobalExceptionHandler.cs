using Banking.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Middleware;

/// <summary>
/// Letzte Verteidigungslinie für Exceptions, die die Application-Schicht nicht bereits in
/// ein Result&lt;T&gt; übersetzt hat. Zwei Fälle:
/// - DomainException: eine Geschäftsregel wurde verletzt, obwohl der Handler das eigentlich
///   hätte abfangen sollen (z. B. eine neue Domain-Regel, die noch nicht in der Application-
///   Schicht berücksichtigt ist) -> 409, mit der Domain-Fehlermeldung, kein Stacktrace-Leak.
/// - alles andere: unerwarteter/technischer Fehler (DB nicht erreichbar, Bug) -> 500,
///   generische Meldung, vollständiges Logging für die Diagnose.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DomainException domainException => (StatusCodes.Status409Conflict, domainException.Message),
            _ => (StatusCodes.Status500InternalServerError, "Es ist ein unerwarteter Fehler aufgetreten."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unbehandelte Ausnahme bei {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Unerwartete Domain-Ausnahme bei {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{statusCode}",
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
