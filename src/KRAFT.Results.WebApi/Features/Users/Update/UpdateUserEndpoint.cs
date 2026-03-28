using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.Update;

internal static class UpdateUserEndpoint
{
    internal const string Name = "UpdateUser";

    internal static RouteGroupBuilder MapUpdateUserEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{userId:int}", static async (
            [FromRoute] int userId,
            [FromBody] UpdateUserCommand command,
            [FromServices] UpdateUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(userId, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.Ok(),
                failure: error => error.Code switch
                {
                    UserErrors.UserNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    UserErrors.EmailExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Updates a user.")
        .WithDescription("Updates an existing user's details.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}