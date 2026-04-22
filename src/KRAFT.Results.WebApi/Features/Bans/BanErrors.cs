using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Bans;

internal static class BanErrors
{
    internal static readonly Error FromDateAfterToDate = new(
        "Bans.FromDateAfterToDate",
        "From date cannot be after to date.");
}