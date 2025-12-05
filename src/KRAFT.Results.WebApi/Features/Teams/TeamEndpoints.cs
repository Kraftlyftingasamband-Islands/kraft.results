using KRAFT.Results.WebApi.Features.Teams.Create;
using KRAFT.Results.WebApi.Features.Teams.Get;
using KRAFT.Results.WebApi.Features.Teams.GetBySlug;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamEndpoints
{
    internal static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/teams")
            .WithTags("Teams");

        group.MapCreateTeamEndpoint();
        group.MapGetTeamsEndpoint();
        group.MapGetTeamDetailsEndpoint();

        return group;
    }
}