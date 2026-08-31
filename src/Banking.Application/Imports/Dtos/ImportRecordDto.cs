using Banking.Domain.Imports;

namespace Banking.Application.Imports.Dtos;

public sealed record ImportRecordDto(
    int RowNumber,
    ImportRecordOutcome Outcome,
    string? ExternalTransactionId,
    string? ErrorMessage);
