using KRAFT.Results.WebApi.Features.Athletes.Create;
using KRAFT.Results.WebApi.Features.Athletes.Get;
using KRAFT.Results.WebApi.Features.Athletes.GetDetails;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteEndpoints
{
    internal static IEndpointRouteBuilder MapAthleteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/athletes")
            .WithTags("Athletes");

        group.MapCreateAthleteEndpoint();
        group.MapGetAthletesEndpoint();
        group.MapGetAthleteDetailsEndpoint();

        return group;
    }
}