using KRAFT.Results.WebApi.Features.Meets.AddParticipant;
using KRAFT.Results.WebApi.Features.Meets.Create;
using KRAFT.Results.WebApi.Features.Meets.Delete;
using KRAFT.Results.WebApi.Features.Meets.Get;
using KRAFT.Results.WebApi.Features.Meets.GetDetails;
using KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;
using KRAFT.Results.WebApi.Features.Meets.GetParticipations;
using KRAFT.Results.WebApi.Features.Meets.GetPendingRecords;
using KRAFT.Results.WebApi.Features.Meets.GetTeamPoints;
using KRAFT.Results.WebApi.Features.Meets.RecordAttempt;
using KRAFT.Results.WebApi.Features.Meets.Update;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetServices
{
    internal static IServiceCollection AddMeets(this IServiceCollection services)
    {
        services.AddScoped<AddParticipantHandler>();
        services.AddScoped<CreateMeetHandler>();
        services.AddScoped<GetMeetTypesHandler>();
        services.AddScoped<GetMeetsHandler>();
        services.AddScoped<GetMeetDetailsHandler>();
        services.AddScoped<GetMeetParticipationsHandler>();
        services.AddScoped<GetMeetTeamPointsHandler>();
        services.AddScoped<GetMeetPendingRecordsHandler>();
        services.AddScoped<UpdateMeetHandler>();
        services.AddScoped<RecordAttemptHandler>();
        services.AddScoped<DeleteMeetHandler>();

        return services;
    }
}