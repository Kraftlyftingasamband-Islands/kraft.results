using KRAFT.Results.WebApi.Features.Athletes.Create;
using KRAFT.Results.WebApi.Features.Athletes.Get;
using KRAFT.Results.WebApi.Features.Athletes.GetDetails;
using KRAFT.Results.WebApi.Features.Athletes.GetParticipations;
using KRAFT.Results.WebApi.Features.Athletes.GetPersonalBests;
using KRAFT.Results.WebApi.Features.Athletes.GetRecords;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteServices
{
    internal static IServiceCollection AddAthletes(this IServiceCollection services)
    {
        services.AddScoped<CreateAthleteHandler>();
        services.AddScoped<GetAthletesHandler>();
        services.AddScoped<GetAthleteDetailsHandler>();
        services.AddScoped<GetAthletePersonalBestsHandler>();
        services.AddScoped<GetAthleteRecordsHandler>();
        services.AddScoped<GetAthleteParticipationsHandler>();

        return services;
    }
}