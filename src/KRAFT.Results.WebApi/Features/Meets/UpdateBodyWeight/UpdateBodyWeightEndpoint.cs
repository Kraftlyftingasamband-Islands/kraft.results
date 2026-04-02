using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.UpdateBodyWeight;

internal static class UpdateBodyWeightEndpoint
{
    internal const string Name = "UpdateBodyWeight";

    internal static RouteGroupBuilder MapUpdateBodyWeightEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPatch("/{meetId:int}/participations/{participationId:int}", static async (
            [FromRoute] int meetId,
            [FromRoute] int participationId,
            [FromBody] UpdateBodyWeightCommand command,
            [FromServices] UpdateBodyWeightHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(meetId, participationId, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    MeetErrors.ParticipationNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Updates the body weight of a participation.")
        .WithDescription("Updates the body weight for a given participation in a meet.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}