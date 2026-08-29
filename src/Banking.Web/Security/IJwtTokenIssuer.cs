using System.Security.Claims;

namespace Banking.Web.Security;

public interface IJwtTokenIssuer
{
    public string IssueToken(ClaimsPrincipal user);
}
