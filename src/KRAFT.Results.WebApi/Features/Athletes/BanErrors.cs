using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class BanErrors
{
    internal static Error FromDateAfterToDate => new(
        "Bans.FromDateAfterToDate",
        "From date cannot be after to date.");
}