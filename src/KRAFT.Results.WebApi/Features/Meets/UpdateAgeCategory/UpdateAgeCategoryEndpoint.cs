using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.UpdateAgeCategory;

internal static class UpdateAgeCategoryEndpoint
{
    internal const string Name = "UpdateAgeCategory";

    internal static RouteGroupBuilder MapUpdateAgeCategoryEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPatch("/{meetId:int}/participations/{participationId:int}/age-category", static async (
            [FromRoute] int meetId,
            [FromRoute] int participationId,
            [FromBody] UpdateAgeCategoryCommand command,
            [FromServices] UpdateAgeCategoryHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(meetId, participationId, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    MeetErrors.ParticipationNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    MeetErrors.AgeCategoryNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Updates the age category of a participation.")
        .WithDescription("Updates the age category for a given participation in a meet.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}