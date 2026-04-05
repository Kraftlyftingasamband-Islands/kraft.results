using KRAFT.Results.WebApi.Features.Meets.AddParticipant;
using KRAFT.Results.WebApi.Features.Meets.ApprovePendingRecord;
using KRAFT.Results.WebApi.Features.Meets.Create;
using KRAFT.Results.WebApi.Features.Meets.Delete;
using KRAFT.Results.WebApi.Features.Meets.Get;
using KRAFT.Results.WebApi.Features.Meets.GetDetails;
using KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;
using KRAFT.Results.WebApi.Features.Meets.GetParticipations;
using KRAFT.Results.WebApi.Features.Meets.GetPendingRecords;
using KRAFT.Results.WebApi.Features.Meets.GetRecords;
using KRAFT.Results.WebApi.Features.Meets.GetTeamPoints;
using KRAFT.Results.WebApi.Features.Meets.RecordAttempt;
using KRAFT.Results.WebApi.Features.Meets.RemoveParticipant;
using KRAFT.Results.WebApi.Features.Meets.Update;
using KRAFT.Results.WebApi.Features.Meets.UpdateAgeCategory;
using KRAFT.Results.WebApi.Features.Meets.UpdateBodyWeight;

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
        group.MapGetMeetTeamPointsEndpoint();
        group.MapGetMeetPendingRecordsEndpoint();
        group.MapGetMeetRecordsEndpoint();
        group.MapApprovePendingRecordEndpoint();
        group.MapUpdateMeetEndpoint();
        group.MapUpdateAgeCategoryEndpoint();
        group.MapUpdateBodyWeightEndpoint();
        group.MapRecordAttemptEndpoint();
        group.MapRemoveParticipantEndpoint();
        group.MapDeleteMeetEndpoint();

        return group;
    }
}