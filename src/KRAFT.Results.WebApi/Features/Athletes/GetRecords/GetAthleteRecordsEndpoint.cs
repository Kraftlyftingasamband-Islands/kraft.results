using KRAFT.Results.Contracts.Athletes;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.GetRecords;

internal static class GetAthleteRecordsEndpoint
{
    internal const string Name = "GetAthleteRecords";

    internal static RouteGroupBuilder MapGetAthleteRecordsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/records", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetAthleteRecordsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets athlete records")
        .WithDescription("Gets a single athlete's records by its slug")
        .Produces<IReadOnlyList<AthleteRecord>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}