using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Meets.AddParticipant;
using KRAFT.Results.WebApi.Features.Meets.Create;
using KRAFT.Results.WebApi.Features.Meets.Delete;
using KRAFT.Results.WebApi.Features.Meets.Get;
using KRAFT.Results.WebApi.Features.Meets.GetDetails;
using KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;
using KRAFT.Results.WebApi.Features.Meets.GetParticipation;
using KRAFT.Results.WebApi.Features.Meets.GetParticipations;
using KRAFT.Results.WebApi.Features.Meets.GetRecords;
using KRAFT.Results.WebApi.Features.Meets.GetTeamPoints;
using KRAFT.Results.WebApi.Features.Meets.RecordAttempt;
using KRAFT.Results.WebApi.Features.Meets.RemoveParticipant;
using KRAFT.Results.WebApi.Features.Meets.Update;
using KRAFT.Results.WebApi.Features.Meets.UpdateAgeCategory;
using KRAFT.Results.WebApi.Features.Meets.UpdateBodyWeight;
using KRAFT.Results.WebApi.Features.Participations.ComputePlaces;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetServices
{
    internal static IServiceCollection AddMeets(this IServiceCollection services)
    {
        services.AddScoped<AddParticipantHandler>();
        services.AddScoped<CreateMeetHandler>();
        services.AddScoped<DeleteMeetHandler>();
        services.AddScoped<GetMeetDetailsHandler>();
        services.AddScoped<GetMeetParticipationHandler>();
        services.AddScoped<GetMeetParticipationsHandler>();
        services.AddScoped<GetMeetRecordsHandler>();
        services.AddScoped<GetMeetsHandler>();
        services.AddScoped<GetMeetTeamPointsHandler>();
        services.AddScoped<GetMeetTypesHandler>();
        services.AddScoped<PlaceComputationService>();
        services.AddScoped<RecordAttemptHandler>();
        services.AddScoped<RemoveParticipantHandler>();
        services.AddScoped<UpdateAgeCategoryHandler>();
        services.AddScoped<UpdateBodyWeightHandler>();
        services.AddScoped<UpdateMeetHandler>();
        services.AddScoped<IDomainEventHandler<CalcPlacesChangedEvent>, CalcPlacesChangedEventHandler>();

        return services;
    }
}