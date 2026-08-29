using Banking.Web.Models;

namespace Banking.Web.Services;

public interface IDashboardService
{
    public Task<ApiResult<DashboardSummary>> GetSummaryAsync(CancellationToken cancellationToken);
}
