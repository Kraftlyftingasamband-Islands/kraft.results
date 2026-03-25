using KRAFT.Results.WebApi.Features.TeamCompetition.Get;

namespace KRAFT.Results.WebApi.Features.TeamCompetition;

internal static class TeamCompetitionEndpoints
{
    internal static IEndpointRouteBuilder MapTeamCompetitionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/team-competition")
            .WithTags("TeamCompetition");

        group.MapGetTeamCompetitionEndpoint();

        return group;
    }
}