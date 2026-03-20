using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace KRAFT.Results.Web.Client.Features.Auth;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly TokenStorageService _tokenStorage;
    private string? _cachedToken;
    private AuthenticationState? _cachedState;

    public JwtAuthenticationStateProvider(TokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? token = await _tokenStorage.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        if (token == _cachedToken && _cachedState is not null)
        {
            return _cachedState;
        }

        return BuildAuthenticationState(token);
    }

    public void MarkUserAsAuthenticated(string token)
    {
        AuthenticationState authState = BuildAuthenticationState(token);
        _cachedToken = token;
        _cachedState = authState;
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public void MarkUserAsLoggedOut()
    {
        _cachedToken = null;
        _cachedState = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private AuthenticationState BuildAuthenticationState(string token)
    {
        IEnumerable<Claim> claims = JwtClaimParser.ParseClaimsFromJwt(token);
        Claim? expClaim = claims.OfType<Claim>().FirstOrDefault(c => c.Type == "exp");

        if (expClaim is not null && long.TryParse(expClaim.Value, out long expUnix))
        {
            DateTimeOffset expiration = DateTimeOffset.FromUnixTimeSeconds(expUnix);

            if (expiration <= DateTimeOffset.UtcNow)
            {
                _cachedToken = null;
                _cachedState = null;
                _tokenStorage.RemoveTokenAsync().ConfigureAwait(false);
                return Anonymous;
            }
        }

        ClaimsIdentity identity = new(claims, "jwt");
        ClaimsPrincipal user = new(identity);
        AuthenticationState state = new(user);

        _cachedToken = token;
        _cachedState = state;

        return state;
    }
}