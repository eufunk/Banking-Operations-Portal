using Banking.Application.Common.Errors;

namespace Banking.Application.Common;

/// <summary>
/// Ergebnis eines Use Cases: entweder ein Wert oder ein erwarteter Fehler - nie beides,
/// nie keins. Ersetzt Exceptions für Fälle, die im normalen Betrieb regelmäßig auftreten
/// (nicht gefunden, Validierungsfehler, Geschäftsregel-Konflikt). Echte Ausnahmesituationen
/// (DB nicht erreichbar, Bug) sollen weiterhin als Exception nach oben durchschlagen.
/// </summary>
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, T value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(Error error) => new(false, default!, error);
}
