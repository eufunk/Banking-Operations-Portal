namespace Banking.Application.Sap;

/// <summary>
/// Technischer Fehler beim Zugriff auf SAP (Timeout, Verbindungsfehler, unerwartete Antwort) -
/// abgegrenzt von einem <c>null</c>-Rückgabewert (fachlich: SAP kennt den Kunden/das Konto
/// schlicht nicht). Aufrufer, die eine externe Abhängigkeit nicht zum Komplettausfall der
/// eigenen Anfrage werden lassen wollen, fangen gezielt diesen Typ ab (siehe
/// GetCustomerSapProfileHandler).
/// </summary>
public sealed class SapIntegrationException : Exception
{
    public SapIntegrationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
