using Banking.Web.Models;

namespace Banking.Web.Services;

public sealed class ImportsApiClient : ApiClientBase, IImportsApiClient
{
    public ImportsApiClient(HttpClient httpClient, ILogger<ImportsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<PagedResult<ImportJobSummary>>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken)
        => GetAsync<PagedResult<ImportJobSummary>>($"api/imports?page={page}&pageSize={pageSize}", cancellationToken);

    public Task<ApiResult<ImportJobDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync<ImportJobDetailsDto>($"api/imports/{id}", cancellationToken);

    public Task<ApiResult<ImportJobSummary>> UploadAsync(string fileName, Stream fileContent, CancellationToken cancellationToken)
        => PostFileAsync<ImportJobSummary>("api/imports", fileName, fileContent, cancellationToken);
}
