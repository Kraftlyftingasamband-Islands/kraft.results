using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features;

internal static class FeatureServices
{
    internal static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddAthletes();
        services.AddCountries();
        services.AddMeets();
        services.AddUsers();
        services.AddTeams();

        return services;
    }
}