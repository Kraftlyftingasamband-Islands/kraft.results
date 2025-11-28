using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace KRAFT.Results.WebApi.Services;

internal sealed class HttpContextService : IHttpContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsInRole(string role)
        => _httpContextAccessor.HttpContext?.User is ClaimsPrincipal user && user.IsInRole(role);

    public string GetUserName()
        => _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Name)
        ?? throw new InvalidOperationException("HTTP context does not contain username");
}