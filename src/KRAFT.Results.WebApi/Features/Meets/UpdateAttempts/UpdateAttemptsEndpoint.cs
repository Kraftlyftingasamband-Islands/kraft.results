using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.UpdateAttempts;

internal static class UpdateAttemptsEndpoint
{
    internal const string Name = "UpdateAttempts";

    internal static RouteGroupBuilder MapUpdateAttemptsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{meetId:int}/participants/{participationId:int}/attempts", static async (
            [FromRoute] int meetId,
            [FromRoute] int participationId,
            [FromBody] UpdateAttemptsCommand command,
            [FromServices] UpdateAttemptsHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(meetId, participationId, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    MeetErrors.ParticipationNotFoundCode => TypedResults.NotFound(error.Description),
                    _ => TypedResults.BadRequest(error.Description),
                });
        })
        .WithName(Name)
        .WithSummary("Updates attempts for a participation.")
        .WithDescription("Replaces all attempts for a participation and recalculates totals.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}