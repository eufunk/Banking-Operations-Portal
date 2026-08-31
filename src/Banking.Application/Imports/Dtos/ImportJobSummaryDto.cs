using Banking.Domain.Imports;

namespace Banking.Application.Imports.Dtos;

public sealed record ImportJobSummaryDto(
    ImportJobId Id,
    string FileName,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalRecords,
    int SuccessfulRecords,
    int FailedRecords,
    int SkippedRecords,
    ImportJobStatus Status);
