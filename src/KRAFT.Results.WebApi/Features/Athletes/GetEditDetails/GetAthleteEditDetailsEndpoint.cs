using KRAFT.Results.Contracts.Athletes;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.GetEditDetails;

internal static class GetAthleteEditDetailsEndpoint
{
    internal const string Name = "GetAthleteEditDetails";

    internal static RouteGroupBuilder MapGetAthleteEditDetailsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/edit", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetAthleteEditDetailsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets athlete edit details")
        .WithDescription("Gets a single athlete's editable details by its slug")
        .Produces<AthleteEditDetails>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}