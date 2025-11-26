using KRAFT.Results.WebApi.Features.Teams.Create;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamServices
{
    internal static IServiceCollection AddTeams(this IServiceCollection services)
    {
        services.AddTransient<CreateTeamHandler>();

        return services;
    }
}