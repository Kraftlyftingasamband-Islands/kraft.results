using KRAFT.Results.Contracts.Teams;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Teams.GetDetails;

internal static class GetTeamDetailsEndpoint
{
    internal const string Name = "GetTeamDetails";

    internal static RouteGroupBuilder MapGetTeamDetailsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetTeamDetailsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets team details")
        .WithDescription("Gets a single team's details by its slug")
        .Produces<TeamDetails>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}