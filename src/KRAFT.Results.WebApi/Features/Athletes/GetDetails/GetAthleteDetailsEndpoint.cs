using KRAFT.Results.Contracts.Athletes;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.GetDetails;

internal static class GetAthleteDetailsEndpoint
{
    internal const string Name = "GetAthleteDetails";

    internal static RouteGroupBuilder MapGetAthleteDetailsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetAthleteDetailsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets athlete details")
        .WithDescription("Gets a single athlete's details by its slug")
        .Produces<AthleteDetails>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}