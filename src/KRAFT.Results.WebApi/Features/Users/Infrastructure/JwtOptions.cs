namespace KRAFT.Results.WebApi.Features.Users.Infrastructure;

internal sealed class JwtOptions
{
    internal const string SectionName = "Jwt";

    public required string Key { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public int ExpirationInMinutes { get; init; }
}