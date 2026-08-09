using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Clubs.Update;

internal static class UpdateClubEndpoint
{
    internal const string Name = "UpdateTeam";

    internal static RouteGroupBuilder MapUpdateClubEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{slug}", static async (
            [FromRoute] string slug,
            [FromBody] UpdateClubCommand command,
            [FromServices] UpdateClubHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.Ok(),
                failure: error => error.Code switch
                {
                    ClubErrors.ClubNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    ClubErrors.ShortTitleExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    ClubErrors.TitleExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
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
