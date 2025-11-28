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
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<AthleteSummary> result = await handler.Handle(cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets athletes")
        .WithDescription("Gets a list of all athletes")
        .Produces<IReadOnlyList<AthleteSummary>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}