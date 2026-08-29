using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace Banking.Web.Security;

/// <summary>
/// Wird auf jeden typisierten HttpClient zur Api registriert (Program.cs). Hängt bei
/// jedem ausgehenden Request das aktuelle Nutzer-JWT an - ohne gültiges/authentifiziertes
/// Cookie gibt es kein Token, und die Api lehnt den Aufruf dann unabhängig davon ab,
/// was die Blazor-Oberfläche gerade anzeigt oder verbirgt.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IJwtTokenIssuer _tokenIssuer;

    public AuthTokenHandler(AuthenticationStateProvider authenticationStateProvider, IJwtTokenIssuer tokenIssuer)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _tokenIssuer = tokenIssuer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var token = _tokenIssuer.IssueToken(user);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
