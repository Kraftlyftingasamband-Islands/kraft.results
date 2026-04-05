using KRAFT.Results.Contracts.Teams;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Teams.GetOptions;

internal static class GetTeamOptionsEndpoint
{
    internal const string Name = "GetTeamOptions";

    internal static RouteGroupBuilder MapGetTeamOptionsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/options", static async (
            [FromServices] GetTeamOptionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<TeamOption> result = await handler.Handle(cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets team options")
        .WithDescription("Gets a list of all teams as id/title pairs for dropdowns")
        .Produces<IReadOnlyList<TeamOption>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        return endpoints;
    }
}