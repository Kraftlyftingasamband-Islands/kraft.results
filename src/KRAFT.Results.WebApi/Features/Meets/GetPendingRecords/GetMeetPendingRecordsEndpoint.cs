using KRAFT.Results.Contracts.Records;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetPendingRecords;

internal static class GetMeetPendingRecordsEndpoint
{
    internal const string Name = "GetMeetPendingRecords";

    internal static RouteGroupBuilder MapGetMeetPendingRecordsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/pending-records", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetMeetPendingRecordsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets pending records for a meet")
        .WithDescription("Gets all records with pending status that are linked to attempts in the specified meet")
        .Produces<List<PendingRecordEntry>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}