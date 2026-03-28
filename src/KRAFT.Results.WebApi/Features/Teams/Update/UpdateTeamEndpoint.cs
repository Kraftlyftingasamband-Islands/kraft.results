using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

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
                    TeamErrors.TeamNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    TeamErrors.ShortTitleExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Updates a team.")
        .WithDescription("Updates an existing team's details.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}