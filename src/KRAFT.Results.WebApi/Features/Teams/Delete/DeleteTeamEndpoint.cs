using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Teams.Delete;

internal static class DeleteTeamEndpoint
{
    internal const string Name = "DeleteTeam";

    internal static RouteGroupBuilder MapDeleteTeamEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapDelete("/{slug}", static async (
            [FromRoute] string slug,
            [FromServices] DeleteTeamHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    TeamErrors.TeamNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    TeamErrors.TeamHasAthletesCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Deletes a team.")
        .WithDescription("Deletes a team if it has no athletes assigned.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}