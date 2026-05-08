using KRAFT.Results.Contracts.Countries;

namespace KRAFT.Results.WebApi.Features.Countries.Get;

internal static class GetCountriesEndpoint
{
    internal const string Name = "GetCountries";

    internal static RouteGroupBuilder MapGetCountriesEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static () =>
        {
            IReadOnlyList<CountrySummary> result = GetCountriesHandler.Handle();

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets countries")
        .WithDescription("Gets a list of countries")
        .Produces<IReadOnlyList<CountrySummary>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}