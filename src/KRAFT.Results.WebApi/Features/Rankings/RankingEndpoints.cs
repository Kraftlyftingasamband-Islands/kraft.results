using KRAFT.Results.WebApi.Features.Rankings.Get;

namespace KRAFT.Results.WebApi.Features.Rankings;

internal static class RankingEndpoints
{
    internal static IEndpointRouteBuilder MapRankingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/rankings")
            .WithTags("Rankings");

        group.MapGetRankingsEndpoint();

        return group;
    }
}