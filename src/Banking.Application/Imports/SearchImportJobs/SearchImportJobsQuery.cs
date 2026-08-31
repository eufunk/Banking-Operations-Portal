using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Imports.Dtos;

namespace Banking.Application.Imports.SearchImportJobs;

public sealed record SearchImportJobsQuery(int Page, int PageSize) : IQuery<PagedResult<ImportJobSummaryDto>>;
