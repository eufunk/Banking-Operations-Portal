namespace Banking.Application.Common.Errors;

/// <summary>
/// Erwarteter, "anticipierter" Fehlerfall (nicht gefunden, ungültige Eingabe, Konflikt).
/// Bewusst kein Exception-Typ: die Api-Schicht kann direkt anhand von <see cref="Type"/>
/// auf den passenden HTTP-Statuscode mappen, ohne Stacktraces/Exception-Handling für
/// Fälle zu brauchen, die im normalen Betrieb regelmäßig vorkommen (z. B. "nicht gefunden").
/// </summary>
public sealed record Error(ErrorType Type, string Code, string Message, IReadOnlyList<string>? Details = null)
{
    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static Error Validation(string code, string message, IReadOnlyList<string>? details = null)
        => new(ErrorType.Validation, code, message, details);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);

    public static Error Unexpected(string code, string message) => new(ErrorType.Unexpected, code, message);
}
