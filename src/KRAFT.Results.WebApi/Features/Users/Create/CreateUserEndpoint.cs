using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.Create;

internal static class CreateUserEndpoint
{
    internal const string Name = "CreateUser";

    internal static RouteGroupBuilder MapCreateUserEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/", static async (
            [FromBody] CreateUserCommand command,
            [FromServices] CreateUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<int> result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: id => TypedResults.Created($"/{id}", new { UserId = id }),
                failure: error => error.Code switch
                {
                    UserErrors.UserNameExistsCode => TypedResults.Conflict(error.Description),
                    UserErrors.EmailExistsCode => TypedResults.Conflict(error.Description),
                    _ => TypedResults.BadRequest(error.Description),
                });
        })
        .WithName(Name)
        .WithSummary("Creates a new user.")
        .WithDescription("Adds a new user to the database and returns its Id.")
        .Produces<int>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        return endpoints;
    }
}