using KRAFT.Results.WebApi.Features.Athletes.Create;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteServices
{
    internal static IServiceCollection AddAthletes(this IServiceCollection services)
    {
        services.AddTransient<CreateAthleteHandler>();

        return services;
    }
}