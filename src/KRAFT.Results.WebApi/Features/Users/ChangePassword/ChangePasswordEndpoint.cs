using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.ChangePassword;

internal static class ChangePasswordEndpoint
{
    internal const string Name = "ChangePassword";

    internal static RouteGroupBuilder MapChangePasswordEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/change-password", static async (
            [FromBody] ChangePasswordCommand command,
            [FromServices] ChangePasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.Ok(),
                failure: error => error.Code switch
                {
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    UserErrors.IncorrectCurrentPasswordCode => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.PasswordsDoNotMatchCode => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Changes the current user's password.")
        .WithDescription("Allows an authenticated user to change their password by providing the current password and a new one.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization()
        .RequireRateLimiting("auth");

        return endpoints;
    }
}