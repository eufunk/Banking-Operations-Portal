using Banking.Web.Models;

namespace Banking.Web.Services;

public interface IImportsApiClient
{
    public Task<ApiResult<PagedResult<ImportJobSummary>>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken);

    public Task<ApiResult<ImportJobDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    public Task<ApiResult<ImportJobSummary>> UploadAsync(string fileName, Stream fileContent, CancellationToken cancellationToken);
}
