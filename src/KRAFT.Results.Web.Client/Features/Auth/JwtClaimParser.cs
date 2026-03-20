using System.Security.Claims;
using System.Text.Json;

namespace KRAFT.Results.Web.Client.Features.Auth;

public static class JwtClaimParser
{
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        ArgumentNullException.ThrowIfNull(jwt);

        string[] parts = jwt.Split('.');

        if (parts.Length != 3)
        {
            return [];
        }

        byte[] payload = ParseBase64WithoutPadding(parts[1]);
        Dictionary<string, JsonElement>? claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);

        if (claims is null)
        {
            return [];
        }

        List<Claim> result = [];

        foreach (KeyValuePair<string, JsonElement> claim in claims)
        {
            if (claim.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement element in claim.Value.EnumerateArray())
                {
                    string value = element.ValueKind == JsonValueKind.String
                        ? element.GetString() ?? string.Empty
                        : element.GetRawText();
                    result.Add(new Claim(claim.Key, value));
                }
            }
            else if (claim.Value.ValueKind == JsonValueKind.String)
            {
                result.Add(new Claim(claim.Key, claim.Value.GetString() ?? string.Empty));
            }
            else
            {
                result.Add(new Claim(claim.Key, claim.Value.GetRawText()));
            }
        }

        return result;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        int remainder = base64.Length % 4;

        if (remainder != 0)
        {
            base64 += new string('=', 4 - remainder);
        }

        return Convert.FromBase64String(base64.Replace('-', '+').Replace('_', '/'));
    }
}