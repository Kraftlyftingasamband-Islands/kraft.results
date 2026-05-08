using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes.Create;
using KRAFT.Results.WebApi.Features.Athletes.Delete;
using KRAFT.Results.WebApi.Features.Athletes.Get;
using KRAFT.Results.WebApi.Features.Athletes.GetDetails;
using KRAFT.Results.WebApi.Features.Athletes.GetEditDetails;
using KRAFT.Results.WebApi.Features.Athletes.GetParticipations;
using KRAFT.Results.WebApi.Features.Athletes.GetPersonalBests;
using KRAFT.Results.WebApi.Features.Athletes.GetRecords;
using KRAFT.Results.WebApi.Features.Athletes.Update;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteServices
{
    internal static IServiceCollection AddAthletes(this IServiceCollection services)
    {
        services.AddScoped<CreateAthleteHandler>();
        services.AddScoped<DeleteAthleteHandler>();
        services.AddScoped<GetAthletesHandler>();
        services.AddScoped<GetAthleteDetailsHandler>();
        services.AddScoped<GetAthletePersonalBestsHandler>();
        services.AddScoped<GetAthleteEditDetailsHandler>();
        services.AddScoped<GetAthleteRecordsHandler>();
        services.AddScoped<GetAthleteParticipationsHandler>();
        services.AddScoped<UpdateAthleteHandler>();
        services.AddScoped<IDomainEventHandler<BanAddedEvent>, BanEventHandler>();
        services.AddScoped<IDomainEventHandler<BanRemovedEvent>, BanEventHandler>();

        return services;
    }
}