using KRAFT.Results.Contracts.TeamCompetition;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.TeamCompetition.Get;

internal static class GetTeamCompetitionEndpoint
{
    internal const string Name = "GetTeamCompetition";

    internal static RouteGroupBuilder MapGetTeamCompetitionEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/{year:int}", static async (
            [FromRoute] int year,
            [FromServices] GetTeamCompetitionHandler handler,
            CancellationToken cancellationToken) =>
        {
            TeamCompetitionResponse response = await handler.Handle(year, cancellationToken);
            return TypedResults.Ok(response);
        })
        .WithName(Name)
        .WithSummary("Gets team competition standings")
        .WithDescription("Gets team competition standings for the specified year")
        .Produces<TeamCompetitionResponse>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}