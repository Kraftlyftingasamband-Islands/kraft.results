using KRAFT.Results.Contracts.Athletes;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.GetPersonalBests;

internal static class GetAthletePersonalBestsEndpoint
{
    internal const string Name = "GetAthletePersonalBests";

    internal static RouteGroupBuilder MapGetAthletePersonalBestsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/personalbests", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetAthletePersonalBestsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets athlete personal bests")
        .WithDescription("Gets a single athlete's personal best bests by its slug")
        .Produces<IReadOnlyList<AthletePersonalBest>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}