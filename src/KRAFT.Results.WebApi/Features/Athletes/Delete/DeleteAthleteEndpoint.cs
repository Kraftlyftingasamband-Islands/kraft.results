using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.Delete;

internal static class DeleteAthleteEndpoint
{
    internal const string Name = "DeleteAthlete";

    internal static RouteGroupBuilder MapDeleteAthleteEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapDelete("/{slug}", static async (
            [FromRoute] string slug,
            [FromServices] DeleteAthleteHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    AthleteErrors.AthleteNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    AthleteErrors.AthleteHasParticipationsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Deletes an athlete.")
        .WithDescription("Deletes an athlete if they have no participations.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}