using Banking.Domain.Common;
using Banking.Domain.Common.Exceptions;

namespace Banking.Domain.Imports;

/// <summary>
/// Bildet einen einzelnen Lauf der (simulierten) Azure-Data-Factory-Pipeline
/// "Blob Storage -> Validation -> Transformation -> Azure SQL" ab (Kapitel 12). Aggregat mit
/// <see cref="ImportRecord"/> als Kind-Entity - das erste Aggregat mit echter Kind-Collection
/// in diesem Projekt, siehe docs/architecture/data-import.md für die Begründung.
/// </summary>
public sealed class ImportJob : Entity<ImportJobId>
{
    private readonly List<ImportRecord> _records = new();

    public string FileName { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public ImportJobStatus Status { get; private set; }
    public int SuccessfulRecords { get; private set; }
    public int FailedRecords { get; private set; }
    public int SkippedRecords { get; private set; }
    public int TotalRecords => SuccessfulRecords + FailedRecords + SkippedRecords;

    public IReadOnlyCollection<ImportRecord> Records => _records;

    // Für EF Core.
    private ImportJob()
    {
        FileName = string.Empty;
    }

    private ImportJob(ImportJobId id, string fileName, DateTime startedAt)
        : base(id)
    {
        FileName = fileName;
        StartedAt = startedAt;
        Status = ImportJobStatus.Started;
    }

    public static ImportJob Start(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Dateiname darf nicht leer sein.", nameof(fileName));

        return new ImportJob(ImportJobId.New(), fileName, DateTime.UtcNow);
    }

    public void BeginProcessing() => TransitionTo(ImportJobStatus.Processing, ImportJobStatus.Started);

    public void RecordSuccess(int rowNumber, string externalTransactionId)
    {
        EnsureProcessing();
        _records.Add(new ImportRecord(Id, rowNumber, ImportRecordOutcome.Success, externalTransactionId, errorMessage: null));
        SuccessfulRecords++;
    }

    public void RecordFailure(int rowNumber, string? externalTransactionId, string errorMessage)
    {
        EnsureProcessing();
        _records.Add(new ImportRecord(Id, rowNumber, ImportRecordOutcome.Failed, externalTransactionId, errorMessage));
        FailedRecords++;
    }

    public void RecordSkipped(int rowNumber, string externalTransactionId)
    {
        EnsureProcessing();
        _records.Add(new ImportRecord(Id, rowNumber, ImportRecordOutcome.Skipped, externalTransactionId, errorMessage: null));
        SkippedRecords++;
    }

    /// <summary>
    /// Ein Lauf, bei dem ausnahmslos jede Zeile fehlgeschlagen ist, gilt als gescheiterter
    /// Pipeline-Lauf (Status Failed) - typischerweise ein Zeichen für ein grundsätzliches
    /// Problem (falsches Dateiformat, komplett falsche Spalten), nicht für einzelne
    /// fehlerhafte Datensätze. Teilweise erfolgreiche Läufe gelten bewusst als Completed,
    /// nicht als Failed - das entspricht dem Verhalten realer ETL-Pipelines, die einzelne
    /// ungültige Datensätze überspringen, statt den ganzen Lauf abzubrechen.
    /// </summary>
    public void Complete()
    {
        EnsureProcessing();
        CompletedAt = DateTime.UtcNow;
        Status = TotalRecords > 0 && SuccessfulRecords == 0 && SkippedRecords == 0
            ? ImportJobStatus.Failed
            : ImportJobStatus.Completed;
    }

    /// <summary>
    /// Die Datei selbst konnte nicht gelesen werden (falsches Format, fehlende Spalten) -
    /// anders als bei <see cref="Complete"/> gibt es hier keine einzelnen Zeilen, über die
    /// Erfolg/Misserfolg entschieden werden könnte. Der Fehler wird trotzdem als
    /// ImportRecord (Zeile 0) festgehalten, damit er in derselben Fehlerliste sichtbar ist.
    /// </summary>
    public void FailCompletely(string reason)
    {
        EnsureProcessing();
        CompletedAt = DateTime.UtcNow;
        Status = ImportJobStatus.Failed;
        _records.Add(new ImportRecord(Id, rowNumber: 0, ImportRecordOutcome.Failed, externalTransactionId: null, reason));
        FailedRecords++;
    }

    private void EnsureProcessing()
    {
        if (Status != ImportJobStatus.Processing)
            throw new InvalidOperationException($"Import-Job {Id} ist nicht im Status 'Processing' (aktuell: {Status}).");
    }

    private void TransitionTo(ImportJobStatus target, params ImportJobStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
            throw new InvalidStatusTransitionException(nameof(ImportJob), Status.ToString(), target.ToString());

        Status = target;
    }
}
