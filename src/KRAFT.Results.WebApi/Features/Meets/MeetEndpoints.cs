using KRAFT.Results.WebApi.Features.Meets.AddParticipant;
using KRAFT.Results.WebApi.Features.Meets.Create;
using KRAFT.Results.WebApi.Features.Meets.Delete;
using KRAFT.Results.WebApi.Features.Meets.Get;
using KRAFT.Results.WebApi.Features.Meets.GetDetails;
using KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;
using KRAFT.Results.WebApi.Features.Meets.GetParticipations;
using KRAFT.Results.WebApi.Features.Meets.RecordAttempt;
using KRAFT.Results.WebApi.Features.Meets.Update;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetEndpoints
{
    internal static IEndpointRouteBuilder MapMeetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/meets")
            .WithTags("Meets");

        group.MapAddParticipantEndpoint();
        group.MapCreateMeetEndpoint();
        group.MapGetMeetTypesEndpoint();
        group.MapGetMeetsEndpoint();
        group.MapGetMeetDetailsEndpoint();
        group.MapGetMeetParticipationsEndpoint();
        group.MapUpdateMeetEndpoint();
        group.MapRecordAttemptEndpoint();
        group.MapDeleteMeetEndpoint();

        return group;
    }
}