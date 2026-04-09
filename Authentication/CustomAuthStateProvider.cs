using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using EstadioApp.Services;
using EstadioApp.Models;

namespace EstadioApp.Authentication;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly UserSessionService _session;

    public CustomAuthStateProvider(UserSessionService session)
    {
        _session = session;
        _session.OnChange += NotifyFromSession;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_session.CurrentUser == null)
        {
            // usuario NO autenticado
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }

        // usuario autenticado
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, _session.CurrentUser.Email),
                new Claim(ClaimTypes.Role, _session.CurrentUser.Role),
                new Claim("uid", _session.CurrentUser.Uid)
            },
            authenticationType: "firebase");

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }

    /// <summary>
    /// 🔥 Método público que FirebaseAuthService PUEDE llamar
    /// </summary>
    public void NotifyUserStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void NotifyFromSession()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
