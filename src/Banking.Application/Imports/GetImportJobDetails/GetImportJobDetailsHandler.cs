using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Imports.Dtos;

namespace Banking.Application.Imports.GetImportJobDetails;

public sealed class GetImportJobDetailsHandler : IQueryHandler<GetImportJobDetailsQuery, ImportJobDetailsDto>
{
    private readonly IImportJobRepository _importJobRepository;

    public GetImportJobDetailsHandler(IImportJobRepository importJobRepository)
    {
        _importJobRepository = importJobRepository;
    }

    public async Task<Result<ImportJobDetailsDto>> Handle(GetImportJobDetailsQuery query, CancellationToken cancellationToken)
    {
        var job = await _importJobRepository.GetByIdAsync(query.ImportJobId, cancellationToken);
        if (job is null)
        {
            return Result<ImportJobDetailsDto>.Failure(Error.NotFound(
                "ImportJob.NotFound", $"Import-Job {query.ImportJobId} wurde nicht gefunden."));
        }

        var records = job.Records
            .OrderBy(r => r.RowNumber)
            .Select(r => new ImportRecordDto(r.RowNumber, r.Outcome, r.ExternalTransactionId, r.ErrorMessage))
            .ToList();

        var dto = new ImportJobDetailsDto(
            job.Id, job.FileName, job.StartedAt, job.CompletedAt,
            job.TotalRecords, job.SuccessfulRecords, job.FailedRecords, job.SkippedRecords, job.Status,
            records);

        return Result<ImportJobDetailsDto>.Success(dto);
    }
}
