using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.Update;

internal static class UpdateAthleteEndpoint
{
    internal const string Name = "UpdateAthlete";

    internal static RouteGroupBuilder MapUpdateAthleteEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPut("/{slug}", static async (
            [FromRoute] string slug,
            [FromBody] UpdateAthleteCommand command,
            [FromServices] UpdateAthleteHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(slug, command, cancellationToken);

            return result.Match<IResult>(
                success: () => TypedResults.Ok(),
                failure: error => error.Code switch
                {
                    AthleteErrors.AthleteNotFoundCode => TypedResults.NotFound(error.Description),
                    _ => TypedResults.BadRequest(error.Description),
                });
        })
        .WithName(Name)
        .WithSummary("Updates an athlete.")
        .WithDescription("Updates an existing athlete's details.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}