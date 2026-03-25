using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.Delete;

internal static class DeleteAthleteEndpoint
{
    internal const string Name = "DeleteAthlete";

    internal static RouteGroupBuilder MapDeleteAthleteEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapDelete("/{id:int}", static async (
            [FromRoute] int id,
            [FromServices] DeleteAthleteHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(id, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    AthleteErrors.AthleteNotFoundCode => TypedResults.NotFound(),
                    AthleteErrors.AthleteHasParticipationsCode => TypedResults.Conflict(),
                    _ => TypedResults.BadRequest(),
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