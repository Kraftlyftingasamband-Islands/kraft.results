using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace KRAFT.Results.WebApi.IntegrationTests;

internal sealed class TestNonAdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    internal const string SchemeName = "IntegrationTestNonAdminScheme";

    public TestNonAdminAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ClaimsIdentity identity = new(
        [
            new Claim(JwtRegisteredClaimNames.Name, Constants.TestUser.Username),
        ],
        "Test");

        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}