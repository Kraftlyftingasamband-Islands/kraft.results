using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.RemoveParticipant;

internal static class RemoveParticipantEndpoint
{
    internal const string Name = "RemoveParticipant";

    internal static RouteGroupBuilder MapRemoveParticipantEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapDelete("/{meetId:int}/participants/{participationId:int}", static async (
            [FromRoute] int meetId,
            [FromRoute] int participationId,
            [FromServices] RemoveParticipantHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(meetId, participationId, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    MeetErrors.ParticipationNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Removes a participant from a meet.")
        .WithDescription("Withdraws an athlete from a meet by removing their participation record.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}