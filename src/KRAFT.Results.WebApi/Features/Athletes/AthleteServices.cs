using KRAFT.Results.WebApi.Features.Athletes.Create;
using KRAFT.Results.WebApi.Features.Athletes.Get;
using KRAFT.Results.WebApi.Features.Athletes.GetDetails;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteServices
{
    internal static IServiceCollection AddAthletes(this IServiceCollection services)
    {
        services.AddScoped<CreateAthleteHandler>();
        services.AddScoped<GetAthletesHandler>();
        services.AddScoped<GetAthleteDetailsHandler>();

        return services;
    }
}