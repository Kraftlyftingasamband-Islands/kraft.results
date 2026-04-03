using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetRecords;

internal static class GetMeetRecordsEndpoint
{
    internal const string Name = "GetMeetRecords";

    internal static RouteGroupBuilder MapGetMeetRecordsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{slug}/records", async Task<IResult> (
            [FromRoute] string slug,
            [FromServices] GetMeetRecordsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(slug, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets approved records for a meet")
        .WithDescription("Gets all non-standard records linked to attempts in the specified meet, excluding TotalWilks and TotalIpfPoints")
        .Produces<List<MeetRecordEntry>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}