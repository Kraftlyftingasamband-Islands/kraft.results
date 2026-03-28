using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.Update;

internal static class UpdateMeetEndpoint
{
    internal const string Name = "UpdateMeet";

    internal static RouteGroupBuilder MapUpdateMeetEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{slug}", static async (
            [FromRoute] string slug,
            [FromBody] UpdateMeetCommand command,
            [FromServices] UpdateMeetHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.Ok(),
                failure: error => error.Code switch
                {
                    MeetErrors.MeetNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    MeetErrors.MeetExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Updates a meet.")
        .WithDescription("Updates an existing meet's details.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}