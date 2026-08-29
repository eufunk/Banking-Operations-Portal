using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Banking.Web.Security;

/// <summary>
/// Web-Seite des BFF-Patterns: der Nutzer ist per Cookie am Blazor-Server angemeldet,
/// aber die Api kennt Cookies nicht. Vor jedem Api-Aufruf mintet diese Klasse ein
/// kurzlebiges JWT aus den bereits vorhandenen Claims des Cookie-Principals - die Api
/// validiert Signatur, Aussteller, Zielgruppe und Ablauf komplett unabhängig
/// (Banking.Api/Program.cs), das ist die eigentliche Sicherheitsgrenze.
/// </summary>
public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private readonly IConfiguration _configuration;

    public JwtTokenIssuer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string IssueToken(ClaimsPrincipal user)
    {
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey ist nicht konfiguriert (siehe User Secrets).");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer ist nicht konfiguriert.");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience ist nicht konfiguriert.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Bewusst kurzlebig: das Token verlässt den Server nie (reiner Server-zu-Server-
        // Aufruf), ein Diebstahl-/Replay-Fenster von wenigen Minuten ist ausreichend eng.
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: user.Claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
