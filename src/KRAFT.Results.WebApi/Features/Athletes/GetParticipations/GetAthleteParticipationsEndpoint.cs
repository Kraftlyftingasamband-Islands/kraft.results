using KRAFT.Results.Contracts.Athletes;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.GetParticipations;

internal static class GetAthleteParticipationsEndpoint
{
    internal const string Name = "GetAthleteParticipations";

    internal static RouteGroupBuilder MapGetAthleteParticipationsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/participations", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetAthleteParticipationsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets athlete participations")
        .WithDescription("Gets a single athlete's participations by its slug")
        .Produces<IReadOnlyList<AthleteParticipation>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}