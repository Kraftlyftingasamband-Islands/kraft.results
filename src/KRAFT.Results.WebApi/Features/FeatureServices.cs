using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features;

internal static class FeatureServices
{
    internal static IServiceCollection AddFeatures(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAthletes();
        services.AddCountries();
        services.AddMeets();
        services.AddUsers(configuration);
        services.AddTeams();

        return services;
    }
}