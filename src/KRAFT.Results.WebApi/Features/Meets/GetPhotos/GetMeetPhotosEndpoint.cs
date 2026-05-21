using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetPhotos;

internal static class GetMeetPhotosEndpoint
{
    internal const string Name = "GetMeetPhotos";

    internal static RouteGroupBuilder MapGetMeetPhotosEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/photos", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetMeetPhotosHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets photos for a meet")
        .WithDescription("Gets all photos for the specified meet, ordered by creation date ascending")
        .Produces<MeetPhotos>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
