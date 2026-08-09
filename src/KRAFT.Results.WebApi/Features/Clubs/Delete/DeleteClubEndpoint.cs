using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Clubs.Delete;

internal static class DeleteClubEndpoint
{
    internal const string Name = "DeleteTeam";

    internal static RouteGroupBuilder MapDeleteClubEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapDelete("/{slug}", static async (
            [FromRoute] string slug,
            [FromServices] DeleteClubHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    ClubErrors.ClubNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    ClubErrors.ClubHasAthletesCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Deletes a club.")
        .WithDescription("Deletes a club if it has no athletes assigned.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}
