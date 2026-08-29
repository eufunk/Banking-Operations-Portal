using Microsoft.AspNetCore.Identity;

namespace Banking.Web.Security;

/// <summary>
/// Ersetzt für dieses Portfolio-Projekt eine echte Nutzerverwaltung (Azure AD/Entra ID
/// oder ASP.NET Core Identity mit eigener DB) - "Simuliere Rollen" aus der Anforderung.
/// Passwörter werden trotzdem korrekt gehasht (PBKDF2 über PasswordHasher&lt;T&gt;), nicht
/// im Klartext verglichen - das Muster ist real, nur der Speicherort (In-Memory statt DB)
/// ist eine bewusste Vereinfachung.
///
/// WICHTIG: "Demo123!" ist ein absichtlich einfaches, öffentlich dokumentiertes Passwort
/// für lokale Testkonten. In einer echten Anwendung wäre das ein schwerer Fehler (siehe
/// Abschnitt "Typische Security-Fehler" in der Projektdokumentation).
/// </summary>
public static class DemoUserStore
{
    private static readonly PasswordHasher<string> Hasher = new();

    public static readonly IReadOnlyList<DemoUser> Users =
    [
        Create("employee", "Demo123!", "Erika Mustermann", Roles.BankEmployee),
        Create("manager", "Demo123!", "Max Operations", Roles.OperationsManager),
        Create("admin", "Demo123!", "System Administrator", Roles.Administrator),
    ];

    public static DemoUser? Validate(string username, string password)
    {
        var user = Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return null;
        }

        var result = Hasher.VerifyHashedPassword(user.Username, user.PasswordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
            ? user
            : null;
    }

    private static DemoUser Create(string username, string plainTextPassword, string displayName, string role)
    {
        var hash = Hasher.HashPassword(username, plainTextPassword);
        return new DemoUser(username, hash, displayName, role);
    }
}
