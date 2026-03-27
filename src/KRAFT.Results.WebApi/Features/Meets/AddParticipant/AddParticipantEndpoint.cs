using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.AddParticipant;

internal static class AddParticipantEndpoint
{
    internal const string Name = "AddParticipant";

    internal static RouteGroupBuilder MapAddParticipantEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/{meetId:int}/participants", static async (
            [FromRoute] int meetId,
            [FromBody] AddParticipantCommand command,
            [FromServices] AddParticipantHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<int> result = await handler.Handle(meetId, command, cancellationToken);

            return result.Match<IResult>(
                success: participationId => TypedResults.Created($"/meets/{meetId}/participants/{participationId}", new AddParticipantResponse(participationId)),
                failure: error => error.Code switch
                {
                    MeetErrors.MeetNotFoundCode => TypedResults.NotFound(error.Description),
                    AthleteErrors.AthleteNotFoundCode => TypedResults.NotFound(error.Description),
                    MeetErrors.AthleteAlreadyRegisteredCode => TypedResults.Conflict(error.Description),
                    _ => TypedResults.BadRequest(error.Description),
                });
        })
        .WithName(Name)
        .WithSummary("Adds a participant to a meet.")
        .WithDescription("Registers an athlete as a participant in a meet.")
        .Produces<AddParticipantResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}