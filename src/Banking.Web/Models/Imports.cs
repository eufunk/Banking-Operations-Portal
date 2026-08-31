namespace Banking.Web.Models;

public sealed record ImportJobSummary(
    Guid Id,
    string FileName,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalRecords,
    int SuccessfulRecords,
    int FailedRecords,
    int SkippedRecords,
    ImportJobStatus Status);

public sealed record ImportJobDetailsDto(
    Guid Id,
    string FileName,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalRecords,
    int SuccessfulRecords,
    int FailedRecords,
    int SkippedRecords,
    ImportJobStatus Status,
    IReadOnlyList<ImportRecord> Records);

public sealed record ImportRecord(
    int RowNumber,
    ImportRecordOutcome Outcome,
    string? ExternalTransactionId,
    string? ErrorMessage);
