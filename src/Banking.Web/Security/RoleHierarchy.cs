namespace Banking.Web.Security;

/// <summary>
/// Bildet die Rollenhierarchie aus dem Anforderungs-Kapitel ab (OperationsManager hat
/// "alle Employee-Rechte" zusätzlich, Administrator "vollständigen Zugriff"). Statt einer
/// eigenen Policy-Engine mit Hierarchie-Logik bekommt jeder Nutzer beim Login einfach alle
/// Rollen-Claims, die seine Rolle einschließt - [Authorize(Roles = "BankEmployee")] auf
/// einem Endpunkt funktioniert dann automatisch auch für OperationsManager/Administrator,
/// weil deren Token die BankEmployee-Claim mitträgt.
/// </summary>
public static class RoleHierarchy
{
    private static readonly Dictionary<string, string[]> Expansions = new()
    {
        [Roles.BankEmployee] = [Roles.BankEmployee],
        [Roles.OperationsManager] = [Roles.BankEmployee, Roles.OperationsManager],
        [Roles.Administrator] = [Roles.BankEmployee, Roles.OperationsManager, Roles.Administrator],
    };

    public static IReadOnlyList<string> Expand(string role)
        => Expansions.TryGetValue(role, out var roles) ? roles : [role];
}
