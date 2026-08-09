using KRAFT.Results.Contracts.Clubs;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Clubs.GetDetails;

internal static class GetClubDetailsEndpoint
{
    internal const string Name = "GetClubDetails";

    internal static RouteGroupBuilder MapGetClubDetailsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetClubDetailsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets team details")
        .WithDescription("Gets a single team's details by its slug")
        .Produces<ClubDetails>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
