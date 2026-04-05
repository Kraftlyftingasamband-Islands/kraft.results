using KRAFT.Results.Contracts.Athletes;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.Get;

internal static class GetAthletesEndpoint
{
    internal const string Name = "GetAthletes";

    internal static RouteGroupBuilder MapGetAthletesEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async (
            [FromServices] GetAthletesHandler handler,
            [FromQuery] string? search,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<AthleteSummary> result = await handler.Handle(search, cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets athletes")
        .WithDescription("Gets a list of all athletes")
        .Produces<IReadOnlyList<AthleteSummary>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}