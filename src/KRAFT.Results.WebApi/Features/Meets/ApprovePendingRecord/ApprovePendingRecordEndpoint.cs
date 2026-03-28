using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.ApprovePendingRecord;

internal static class ApprovePendingRecordEndpoint
{
    internal const string Name = "ApprovePendingRecord";

    internal static RouteGroupBuilder MapApprovePendingRecordEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{slug}/pending-records/{attemptId:int}/approve", static async (
            [FromRoute] string slug,
            [FromRoute] int attemptId,
            [FromServices] ApprovePendingRecordHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, attemptId, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    ApprovePendingRecordHandler.AttemptNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Approves a pending record for a meet.")
        .WithDescription("Creates a new record row for a good-lift attempt that exceeds the current record.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}