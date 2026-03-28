using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Athletes.Create;

internal static class CreateAthleteEndpoint
{
    internal const string Name = "CreateAthlete";

    internal static RouteGroupBuilder MapCreateAthleteEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/", static async (
            [FromBody] CreateAthleteCommand command,
            [FromServices] CreateAthleteHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<int> result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: athleteId => TypedResults.Created($"/{athleteId}", new { AthleteId = athleteId }),
                failure: error => error.Code switch
                {
                    AthleteErrors.AlreadyExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Creates a new athlete.")
        .WithDescription("Adds a new athlete to the database and returns its Id.")
        .Produces<int>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}