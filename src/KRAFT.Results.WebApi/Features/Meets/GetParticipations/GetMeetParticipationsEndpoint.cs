using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetParticipations;

internal static class GetMeetParticipationsEndpoint
{
    internal const string Name = "GetMeetParticipations";

    internal static RouteGroupBuilder MapGetMeetParticipationsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/participations", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetMeetParticipationsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets meet participations")
        .WithDescription("Gets a single meet's participations by its slug")
        .Produces<IReadOnlyList<MeetParticipation>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}