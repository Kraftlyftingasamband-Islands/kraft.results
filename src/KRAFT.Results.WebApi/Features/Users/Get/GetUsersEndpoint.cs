using KRAFT.Results.Contracts.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.Get;

internal static class GetUsersEndpoint
{
    internal const string Name = "GetUsers";

    internal static RouteGroupBuilder MapGetUsersEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async (
            [FromServices] GetUsersHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<UserSummary> result = await handler.Handle(cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets users")
        .WithDescription("Gets a list of all users")
        .Produces<IReadOnlyList<UserSummary>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        return endpoints;
    }
}