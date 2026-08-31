using Banking.Application.Common;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Imports.Dtos;

namespace Banking.Application.Imports.SearchImportJobs;

public sealed class SearchImportJobsHandler : IQueryHandler<SearchImportJobsQuery, PagedResult<ImportJobSummaryDto>>
{
    private readonly IImportJobRepository _importJobRepository;

    public SearchImportJobsHandler(IImportJobRepository importJobRepository)
    {
        _importJobRepository = importJobRepository;
    }

    public async Task<Result<PagedResult<ImportJobSummaryDto>>> Handle(SearchImportJobsQuery query, CancellationToken cancellationToken)
    {
        var jobs = await _importJobRepository.SearchAsync(query.Page, query.PageSize, cancellationToken);

        var items = jobs.Items
            .Select(j => new ImportJobSummaryDto(
                j.Id, j.FileName, j.StartedAt, j.CompletedAt,
                j.TotalRecords, j.SuccessfulRecords, j.FailedRecords, j.SkippedRecords, j.Status))
            .ToList();

        return Result<PagedResult<ImportJobSummaryDto>>.Success(
            new PagedResult<ImportJobSummaryDto>(items, jobs.TotalCount, jobs.Page, jobs.PageSize));
    }
}
