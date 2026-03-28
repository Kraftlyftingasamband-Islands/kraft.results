using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.RecordAttempt;

internal static class RecordAttemptEndpoint
{
    internal const string Name = "RecordAttempt";

    internal static RouteGroupBuilder MapRecordAttemptEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{meetId:int}/participants/{participationId:int}/attempts/{discipline:int}/{round:int}", static async (
            [FromRoute] int meetId,
            [FromRoute] int participationId,
            [FromRoute] int discipline,
            [FromRoute] short round,
            [FromBody] RecordAttemptCommand command,
            [FromServices] RecordAttemptHandler handler,
            CancellationToken cancellationToken) =>
        {
            Discipline parsedDiscipline = (Discipline)discipline;

            Result result = await handler.Handle(meetId, participationId, parsedDiscipline, round, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    MeetErrors.ParticipationNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Records or updates an attempt for a participation.")
        .WithDescription("Creates or updates a single attempt for a given discipline and round, then recalculates totals.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}