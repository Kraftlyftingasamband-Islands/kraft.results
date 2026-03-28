using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Records.Reject;

internal static class RejectRecordEndpoint
{
    internal const string Name = "RejectRecord";

    internal static RouteGroupBuilder MapRejectRecordEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{recordId:int}/reject", static async (
            [FromRoute] int recordId,
            [FromBody] RejectRecordCommand command,
            [FromServices] RejectRecordHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(recordId, command.Reason, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    RecordErrors.RecordNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Rejects a pending record.")
        .WithDescription("Changes the status of a pending record to rejected, with an optional reason.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}