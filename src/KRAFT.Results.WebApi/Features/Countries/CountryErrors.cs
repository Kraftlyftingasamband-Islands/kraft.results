using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Countries;

internal static class CountryErrors
{
    internal static Error CountryDoesNotExist(int id) => new(
        "Athletes.CountryDoesNotExist",
        $"Country with Id {id} does not exist.");
}