using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.IntegrationTests;

internal static class FeatureServices
{
    internal static IServiceCollection AddFeatures(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAthletes();
        services.AddUsers(configuration);
        services.AddTeams();
        services.AddMeets();

        return services;
    }
}