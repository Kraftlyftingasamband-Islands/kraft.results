using KRAFT.Results.Contracts.Teams;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Clubs.Get;

internal static class GetClubsEndpoint
{
    internal const string Name = "GetTeams";

    internal static RouteGroupBuilder MapGetClubsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async (
            [FromServices] GetClubsHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<TeamSummary> result = await handler.Handle(cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets teams")
        .WithDescription("Gets a list of all teams")
        .Produces<IReadOnlyList<TeamSummary>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
