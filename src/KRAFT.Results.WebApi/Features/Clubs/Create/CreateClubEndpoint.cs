using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Clubs.Create;

internal static class CreateClubEndpoint
{
    internal const string Name = "CreateTeam";

    internal static RouteGroupBuilder MapCreateClubEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/", static async (
            [FromBody] CreateTeamCommand command,
            [FromServices] CreateClubHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<int> result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: clubId => TypedResults.Created($"/{clubId}", new { TeamId = clubId }),
                failure: error => error.Code switch
                {
                    ClubErrors.ShortTitleExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    ClubErrors.TitleExistsCode => TypedResults.Conflict(new ErrorResponse(error.Code, error.Description)),
                    UserErrors.UserNameClaimMissingCode => TypedResults.Unauthorized(),
                    _ => TypedResults.BadRequest(new ErrorResponse(error.Code, error.Description)),
                });
        })
        .WithName(Name)
        .WithSummary("Creates a new Team.")
        .WithDescription("Adds a new Team to the database and returns its Id.")
        .Produces<int>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }
}
