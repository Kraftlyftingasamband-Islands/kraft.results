using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.Delete;

internal static class DeleteMeetEndpoint
{
    internal const string Name = "DeleteMeet";

    internal static RouteGroupBuilder MapDeleteMeetEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapDelete("/{slug}", static async (
            [FromRoute] string slug,
            [FromServices] DeleteMeetHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    MeetErrors.MeetNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    MeetErrors.MeetHasParticipationsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Deletes a meet.")
        .WithDescription("Deletes a meet if it has no participations.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}