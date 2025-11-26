using KRAFT.Results.WebApi.Features.Teams.Create;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamEndpoints
{
    internal static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/teams")
            .WithTags("Teams");

        group.MapCreateTeamEndpoint();

        return group;
    }
}