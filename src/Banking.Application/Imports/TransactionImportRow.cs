namespace Banking.Application.Imports;

/// <summary>
/// Rohformat einer CSV-Zeile aus transactions_YYYY_MM_DD.csv (Kapitel 12), 1:1 wie sie von
/// CsvHelper eingelesen wird - bewusst alle Felder als string, damit die eigentliche
/// Validierung/Konvertierung explizit und kontrolliert im ImportTransactionsCsvHandler
/// passiert (Validation-Schritt der Pipeline), statt CsvHelper bei einem Typfehler die
/// ganze Zeile hart abbrechen zu lassen.
/// </summary>
public sealed class TransactionImportRow
{
    public string TransactionId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string BookingDate { get; set; } = string.Empty;
    public string? Description { get; set; }
}
