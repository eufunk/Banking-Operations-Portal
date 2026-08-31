using Banking.Domain.Common;

namespace Banking.Domain.Imports;

/// <summary>
/// Ergebnis der Verarbeitung genau einer CSV-Zeile innerhalb eines <see cref="ImportJob"/>.
/// Kind-Entity, kein eigenes Aggregat - wird ausschließlich über den ImportJob erzeugt und
/// gespeichert (siehe <see cref="ImportJob.RecordSuccess"/>/<see cref="ImportJob.RecordFailure"/>/<see cref="ImportJob.RecordSkipped"/>).
/// </summary>
public sealed class ImportRecord : Entity<ImportRecordId>
{
    public ImportJobId ImportJobId { get; private set; }
    public int RowNumber { get; private set; }
    public ImportRecordOutcome Outcome { get; private set; }
    public string? ExternalTransactionId { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Für EF Core.
    private ImportRecord()
    {
    }

    internal ImportRecord(
        ImportJobId importJobId,
        int rowNumber,
        ImportRecordOutcome outcome,
        string? externalTransactionId,
        string? errorMessage)
        : base(ImportRecordId.New())
    {
        ImportJobId = importJobId;
        RowNumber = rowNumber;
        Outcome = outcome;
        ExternalTransactionId = externalTransactionId;
        ErrorMessage = errorMessage;
    }
}
