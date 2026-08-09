using KRAFT.Results.WebApi.Features.Clubs.Create;
using KRAFT.Results.WebApi.Features.Clubs.Delete;
using KRAFT.Results.WebApi.Features.Clubs.Get;
using KRAFT.Results.WebApi.Features.Clubs.GetDetails;
using KRAFT.Results.WebApi.Features.Clubs.GetOptions;
using KRAFT.Results.WebApi.Features.Clubs.Update;

namespace KRAFT.Results.WebApi.Features.Clubs;

internal static class ClubEndpoints
{
    internal static IEndpointRouteBuilder MapClubEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/teams")
            .WithTags("Teams");

        group.MapCreateClubEndpoint();
        group.MapGetClubsEndpoint();
        group.MapGetClubOptionsEndpoint();
        group.MapGetClubDetailsEndpoint();
        group.MapUpdateClubEndpoint();
        group.MapDeleteClubEndpoint();

        return group;
    }
}
