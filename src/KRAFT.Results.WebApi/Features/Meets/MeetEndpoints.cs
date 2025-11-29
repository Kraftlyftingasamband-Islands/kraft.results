using KRAFT.Results.WebApi.Features.Meets.Create;
using KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetEndpoints
{
    internal static IEndpointRouteBuilder MapMeetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/meets")
            .WithTags("Meets");

        group.MapCreateMeetEndpoint();
        group.MapGetMeetTypesEndpoint();

        return group;
    }
}