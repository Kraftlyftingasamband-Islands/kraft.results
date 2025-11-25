using KRAFT.Results.WebApi.Features.Athletes.Create;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteEndpoints
{
    internal static IEndpointRouteBuilder MapAthleteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/athletes")
            .WithTags("Athletes");

        group.MapCreateAthleteEndpoint();

        return group;
    }
}