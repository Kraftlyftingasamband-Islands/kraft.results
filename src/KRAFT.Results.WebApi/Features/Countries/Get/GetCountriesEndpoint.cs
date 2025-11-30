using KRAFT.Results.Contracts.Countries;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Countries.Get;

internal static class GetCountriesEndpoint
{
    internal const string Name = "GetCountries";

    internal static RouteGroupBuilder MapGetCountriesEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async (
            [FromServices] GetCountriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<CountrySummary> result = await handler.Handle(cancellationToken);

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