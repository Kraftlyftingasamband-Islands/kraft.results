using KRAFT.Results.Contracts.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Users.GetEditDetails;

internal static class GetUserEditDetailsEndpoint
{
    internal const string Name = "GetUserEditDetails";

    internal static RouteGroupBuilder MapGetUserEditDetailsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{userId:int}/edit", async Task<IResult> (
            [FromRoute] int userId,
            [FromServices] GetUserEditDetailsHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(userId, cancellationToken) is not { } result
                ? TypedResults.NotFound()
                : TypedResults.Ok(result))
        .WithName(Name)
        .WithSummary("Gets user edit details")
        .WithDescription("Gets a single user's editable details by its id")
        .Produces<UserEditDetails>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}