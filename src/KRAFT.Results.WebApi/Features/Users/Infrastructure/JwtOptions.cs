using System.ComponentModel.DataAnnotations;

namespace KRAFT.Results.WebApi.Features.Users.Infrastructure;

internal sealed class JwtOptions
{
    internal const string SectionName = "Jwt";

    [MinLength(1)]
    public required string Key { get; init; }

    [MinLength(1)]
    public required string Issuer { get; init; }

    [MinLength(1)]
    public required string Audience { get; init; }

    public int ExpirationInMinutes { get; init; }
}