namespace Banking.Api.Security;

/// <summary>
/// Bewusst dupliziert in Banking.Web (nicht als gemeinsame Bibliothek extrahiert) -
/// ADR-0002 verbietet eine gemeinsame Projektreferenz zwischen Web und Api/Domain.
/// Drei Konstanten sind ein akzeptabler Preis für diese Grenze.
/// </summary>
public static class Roles
{
    public const string BankEmployee = "BankEmployee";
    public const string OperationsManager = "OperationsManager";
    public const string Administrator = "Administrator";
}
