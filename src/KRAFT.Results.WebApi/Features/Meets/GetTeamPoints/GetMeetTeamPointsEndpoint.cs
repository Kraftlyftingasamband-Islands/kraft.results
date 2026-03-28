using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetTeamPoints;

internal static class GetMeetTeamPointsEndpoint
{
    internal const string Name = "GetMeetTeamPoints";

    internal static RouteGroupBuilder MapGetMeetTeamPointsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/team-points", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetMeetTeamPointsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets team points for a meet")
        .WithDescription("Gets team point standings for a single meet by its slug")
        .Produces<MeetTeamPointsResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}