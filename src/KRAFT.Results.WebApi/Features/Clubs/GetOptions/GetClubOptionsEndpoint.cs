using KRAFT.Results.Contracts.Teams;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Clubs.GetOptions;

internal static class GetClubOptionsEndpoint
{
    internal const string Name = "GetTeamOptions";

    internal static RouteGroupBuilder MapGetClubOptionsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/options", static async (
            [FromServices] GetClubOptionsHandler handler,
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
