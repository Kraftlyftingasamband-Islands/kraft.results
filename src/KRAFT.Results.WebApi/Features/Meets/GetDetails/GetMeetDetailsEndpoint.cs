using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetDetails;

internal static class GetMeetDetailsEndpoint
{
    internal const string Name = "GetMeetDetails";

    internal static RouteGroupBuilder MapGetMeetDetailsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetMeetDetailsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets meet details")
        .WithDescription("Gets a single meet's details by its slug")
        .Produces<MeetDetails>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}