using KRAFT.Results.Contracts.Countries;

using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.Countries.Get;

internal static class GetCountriesHandler
{
    internal static IReadOnlyList<CountrySummary> Handle() => Country.GetAll();
}