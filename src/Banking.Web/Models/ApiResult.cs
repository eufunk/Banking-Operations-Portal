namespace Banking.Web.Models;

/// <summary>
/// Web-seitiges Gegenstück zu Banking.Application.Common.Result&lt;T&gt; - übersetzt eine
/// HTTP-Antwort (Erfolg oder ProblemDetails) in eine Form, die Komponenten ohne
/// try/catch konsumieren können.
/// </summary>
public sealed class ApiResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<string>? ErrorDetails { get; }
    public int? StatusCode { get; }

    private ApiResult(bool isSuccess, T? value, string? errorMessage, IReadOnlyList<string>? errorDetails, int? statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        StatusCode = statusCode;
    }

    public static ApiResult<T> Success(T value) => new(true, value, null, null, null);

    public static ApiResult<T> Failure(string errorMessage, int? statusCode, IReadOnlyList<string>? details = null)
        => new(false, default, errorMessage, details, statusCode);
}

public sealed class ProblemDetailsResponse
{
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Code { get; set; }
    public List<string>? Errors { get; set; }
}
