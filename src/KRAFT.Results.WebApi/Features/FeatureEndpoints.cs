using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features;

internal static class FeatureEndpoints
{
    internal static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapAthleteEndpoints();
        builder.MapTeamEndpoints();
        builder.MapUserEndpoints();
        builder.MapMeetEndpoints();

        return builder;
    }
}