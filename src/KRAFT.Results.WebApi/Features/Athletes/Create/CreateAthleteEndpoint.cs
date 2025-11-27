using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Athletes.Create;

internal static class CreateAthleteEndpoint
{
    internal const string Name = "CreateAthlete";

    internal static RouteGroupBuilder MapCreateAthleteEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapPost("/", static async (CreateAthleteCommand command, CreateAthleteHandler handler) =>
        {
            Result<int> result = await handler.Handle(command);

            return result.Match<IResult>(
                success: athleteId => TypedResults.Created($"/{athleteId}", new { AthleteId = athleteId }),
                failure: error => error.Code switch
                {
                    _ => TypedResults.BadRequest(error.Description),
                });
        })
        .WithName(Name)
        .WithSummary("Creates a new athlete.")
        .WithDescription("Adds a new athlete to the database and returns its Id.")
        .Produces<int>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        return endpoints;
    }
}