using System.Globalization;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace KRAFT.Results.WebApi.Features.Users.Infrastructure;

internal sealed class TokenProvider
{
    private readonly IOptions<JwtOptions> _options;

    public TokenProvider(IOptions<JwtOptions> jwtOptions)
    {
        _options = jwtOptions;
    }

    public string CreateToken(User user)
    {
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_options.Value.Key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Name, user.Username.ToString(CultureInfo.InvariantCulture)),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(_options.Value.ExpirationInMinutes),
            SigningCredentials = credentials,
            Issuer = _options.Value.Issuer,
            Audience = _options.Value.Audience,
        };

        JsonWebTokenHandler tokenHandler = new();

        string token = tokenHandler.CreateToken(tokenDescriptor);

        return token;
    }
}