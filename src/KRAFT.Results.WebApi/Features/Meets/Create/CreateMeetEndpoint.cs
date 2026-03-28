using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.Create;

internal static class CreateMeetEndpoint
{
    internal const string Name = "CreateMeet";

    internal static RouteGroupBuilder MapCreateMeetEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/", static async (
            [FromBody] CreateMeetCommand command,
            [FromServices] CreateMeetHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<string> result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: slug => TypedResults.Created($"/{slug}", new { Slug = slug }),
                failure: error => error.Code switch
                {
                    MeetErrors.MeetExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Creates a new Meet.")
        .WithDescription("Adds a new Meet to the database and returns its Id.")
        .Produces<int>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}