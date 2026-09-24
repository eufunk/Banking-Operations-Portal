namespace Banking.IntegrationTests;

/// <summary>Spiegelt die flach serialisierten Extensions-Felder ("code", "errors") der
/// ProblemDetails-Antworten der Api - siehe Banking.Api/Extensions/ResultExtensions.cs.</summary>
public sealed class ProblemDetailsResponse
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Code { get; set; }
    public List<string>? Errors { get; set; }
}
