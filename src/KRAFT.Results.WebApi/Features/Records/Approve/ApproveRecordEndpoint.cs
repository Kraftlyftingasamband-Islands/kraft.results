using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Records.Approve;

internal static class ApproveRecordEndpoint
{
    internal const string Name = "ApproveRecord";

    internal static RouteGroupBuilder MapApproveRecordEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{recordId:int}/approve", static async (
            [FromRoute] int recordId,
            [FromServices] ApproveRecordHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(recordId, cancellationToken);

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
        .WithSummary("Approves a pending record.")
        .WithDescription("Changes the status of a pending record to approved.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}