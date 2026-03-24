using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Teams.Update;

internal static class UpdateTeamEndpoint
{
    internal const string Name = "UpdateTeam";

    internal static RouteGroupBuilder MapUpdateTeamEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{slug}", static async (
            [FromRoute] string slug,
            [FromBody] UpdateTeamCommand command,
            [FromServices] UpdateTeamHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.Ok(),
                failure: error => error.Code switch
                {
                    TeamErrors.TeamNotFoundCode => TypedResults.NotFound(),
                    TeamErrors.EmptyTitleCode => TypedResults.BadRequest(error.Description),
                    TeamErrors.InvalidTitleShortCode => TypedResults.BadRequest(error.Description),
                    TeamErrors.EmptyTitleFullCode => TypedResults.BadRequest(error.Description),
                    TeamErrors.TitleTooLongCode => TypedResults.BadRequest(error.Description),
                    TeamErrors.TitleFullTooLongCode => TypedResults.BadRequest(error.Description),
                    _ => TypedResults.BadRequest("Invalid request."),
                });
        })
        .WithName(Name)
        .WithSummary("Updates a team.")
        .WithDescription("Updates an existing team's details.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}