using System.Net.Http.Json;
using Banking.Web.Models;

namespace Banking.Web.Services;

/// <summary>
/// Einzige Stelle, an der eine HTTP-Antwort der Api in ein ApiResult&lt;T&gt; übersetzt wird -
/// jeder typisierte Client (Customers/Accounts/Transactions/Payments) erbt davon, statt
/// dieselbe Fehlerbehandlung viermal zu duplizieren.
/// </summary>
public abstract class ApiClientBase
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger _logger;

    protected ApiClientBase(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient;
        _logger = logger;
    }

    protected async Task<ApiResult<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(requestUri, cancellationToken);
        return await ToApiResultAsync<T>(response, cancellationToken);
    }

    protected async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string requestUri, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.PostAsJsonAsync(requestUri, request, ApiJsonOptions.Default, cancellationToken);
        return await ToApiResultAsync<TResponse>(response, cancellationToken);
    }

    /// <summary>Für POSTs ohne Body (z. B. Statusübergänge wie Approve/Reject).</summary>
    protected async Task<ApiResult<TResponse>> PostAsync<TResponse>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.PostAsync(requestUri, content: null, cancellationToken);
        return await ToApiResultAsync<TResponse>(response, cancellationToken);
    }

    private async Task<ApiResult<T>> ToApiResultAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(ApiJsonOptions.Default, cancellationToken);
            return ApiResult<T>.Success(value!);
        }

        var statusCode = (int)response.StatusCode;

        ProblemDetailsResponse? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(ApiJsonOptions.Default, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehlerantwort der Api ({StatusCode}) konnte nicht als ProblemDetails gelesen werden.", statusCode);
        }

        var message = problem?.Title ?? $"Die Api hat mit Status {statusCode} geantwortet.";

        if (statusCode >= 500)
        {
            _logger.LogError("Api-Fehler {StatusCode} bei {Uri}: {Message}", statusCode, response.RequestMessage?.RequestUri, message);
        }

        return ApiResult<T>.Failure(message, statusCode, problem?.Errors);
    }
}
