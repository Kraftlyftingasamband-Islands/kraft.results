using System.Globalization;
using System.Security.Claims;
using System.Text;

using KRAFT.Results.WebApi.Features.UserRoles;

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

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Name, user.Username.ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Iss, _options.Value.Issuer),
            new Claim(JwtRegisteredClaimNames.Aud, _options.Value.Audience),
        ];

        foreach (UserRole userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleName));
        }

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
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