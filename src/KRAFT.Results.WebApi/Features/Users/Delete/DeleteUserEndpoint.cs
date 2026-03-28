using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.Delete;

internal static class DeleteUserEndpoint
{
    internal const string Name = "DeleteUser";

    internal static RouteGroupBuilder MapDeleteUserEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapDelete("/{userId:int}", static async (
            [FromRoute] int userId,
            [FromServices] DeleteUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(userId, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.NoContent(),
                failure: error => error.Code switch
                {
                    UserErrors.UserNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.CannotDeleteSelfCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Deletes a user.")
        .WithDescription("Deletes a user account. An admin cannot delete their own account.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}