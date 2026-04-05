using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetParticipation;

internal static class GetMeetParticipationEndpoint
{
    internal const string Name = "GetMeetParticipation";

    internal static RouteGroupBuilder MapGetMeetParticipationEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{meetId:int}/participations/{participationId:int}", async Task<IResult> (
            [FromRoute] int meetId,
            [FromRoute] int participationId,
            [FromServices] GetMeetParticipationHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(meetId, participationId, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets a single meet participation")
        .WithDescription("Gets a single participation by meetId and participationId, with computed total and IPF points.")
        .Produces<MeetParticipation>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}