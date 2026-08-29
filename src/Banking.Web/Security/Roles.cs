namespace Banking.Web.Security;

/// <summary>Bewusst dupliziert in Banking.Api - siehe dortige Roles.cs für die Begründung (ADR-0002).</summary>
public static class Roles
{
    public const string BankEmployee = "BankEmployee";
    public const string OperationsManager = "OperationsManager";
    public const string Administrator = "Administrator";
}
