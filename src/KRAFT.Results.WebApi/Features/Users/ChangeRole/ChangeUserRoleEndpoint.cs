using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.ChangeRole;

internal static class ChangeUserRoleEndpoint
{
    internal const string Name = "ChangeUserRole";

    internal static RouteGroupBuilder MapChangeUserRoleEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPatch("/{userId:int}/role", static async (
            [FromRoute] int userId,
            [FromBody] ChangeUserRoleCommand command,
            [FromServices] ChangeUserRoleHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(userId, command.Roles, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.Ok(),
                failure: error => error.Code switch
                {
                    UserErrors.UserNotFoundCode => TypedResults.NotFound(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.CannotChangeOwnRoleCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    UserErrors.RoleNotFoundCode => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.RolesRequiredCode => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Changes a user's roles.")
        .WithDescription("Changes a user's roles. An admin cannot change their own roles.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}