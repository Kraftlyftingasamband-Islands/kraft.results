using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.Login;

internal static class LoginEndpoint
{
    internal const string Name = "Login";

    internal static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/login", static async (
            [FromBody] LoginCommand command,
            [FromServices] LoginHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<AuthenticatedResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: response => TypedResults.Ok(response),
                failure: error => error.Code switch
                {
                    UserErrors.InvalidUsernameOrPasswordCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Authenticates a user and returns a JWT token.")
        .WithDescription("Validates the provided username and password, and returns a JWT token if authentication is successful.")
        .Produces<AuthenticatedResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}