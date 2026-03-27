using KRAFT.Results.Contracts.Records;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Records.GetHistory;

internal static class GetRecordHistoryEndpoint
{
    internal const string Name = "GetRecordHistory";

    internal static RouteGroupBuilder MapGetRecordHistoryEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{id:int}/history", async Task<IResult> (
            [FromRoute] int id,
            [FromServices] GetRecordHistoryHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(id, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets record history")
        .WithDescription("Gets the full history of a record chain by any record ID in the chain")
        .Produces<RecordHistoryResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}