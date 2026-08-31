namespace Banking.Domain.Imports;

/// <summary>Ergebnis der Verarbeitung einer einzelnen CSV-Zeile.</summary>
public enum ImportRecordOutcome
{
    Success,
    Failed,

    /// <summary>Zeile wurde bereits in einem früheren Import-Lauf erfolgreich verarbeitet
    /// (dieselbe externe TransactionId) - Idempotenz, falls dieselbe Datei erneut geliefert
    /// wird oder eine Pipeline nach einem Fehler wiederholt wird.</summary>
    Skipped,
}
