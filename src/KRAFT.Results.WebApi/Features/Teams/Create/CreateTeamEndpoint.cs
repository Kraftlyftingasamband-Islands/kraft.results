using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Teams.Create;

internal static class CreateTeamEndpoint
{
    internal const string Name = "CreateTeam";

    internal static RouteGroupBuilder MapCreateTeamEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/", static async (CreateTeamCommand command, CreateTeamHandler handler, CancellationToken cancellationToken) =>
        {
            Result<int> result = await handler.Handle(command, cancellationToken);

            return result.Match<IResult>(
                success: teamId => TypedResults.Created($"/{teamId}", new { TeamId = teamId }),
                failure: error => error.Code switch
                {
                    _ => TypedResults.BadRequest(error.Description),
                });
        })
        .WithName(Name)
        .WithSummary("Creates a new Team.")
        .WithDescription("Adds a new Team to the database and returns its Id.")
        .Produces<int>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        return endpoints;
    }
}