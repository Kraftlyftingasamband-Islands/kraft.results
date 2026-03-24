using KRAFT.Results.WebApi.Features.Teams.Create;
using KRAFT.Results.WebApi.Features.Teams.Delete;
using KRAFT.Results.WebApi.Features.Teams.Get;
using KRAFT.Results.WebApi.Features.Teams.GetDetails;
using KRAFT.Results.WebApi.Features.Teams.GetOptions;
using KRAFT.Results.WebApi.Features.Teams.Update;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamEndpoints
{
    internal static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/teams")
            .WithTags("Teams");

        group.MapCreateTeamEndpoint();
        group.MapGetTeamsEndpoint();
        group.MapGetTeamOptionsEndpoint();
        group.MapGetTeamDetailsEndpoint();
        group.MapUpdateTeamEndpoint();
        group.MapDeleteTeamEndpoint();

        return group;
    }
}