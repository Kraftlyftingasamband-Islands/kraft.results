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
            Result<int> result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: id => TypedResults.Created($"/{id}", new { MeetId = id }),
                failure: error => error.Code switch
                {
                    MeetErrors.MeetExistsCode => TypedResults.Conflict(),
                    _ => TypedResults.BadRequest(error.Description),
                });
        })
        .WithName(Name)
        .WithSummary("Creates a new Meet.")
        .WithDescription("Adds a new Meet to the database and returns its Id.")
        .Produces<int>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        return endpoints;
    }
}