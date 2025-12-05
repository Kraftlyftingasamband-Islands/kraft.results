using KRAFT.Results.WebApi.Features.Teams.Create;
using KRAFT.Results.WebApi.Features.Teams.Get;
using KRAFT.Results.WebApi.Features.Teams.GetDetails;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamServices
{
    internal static IServiceCollection AddTeams(this IServiceCollection services)
    {
        services.AddScoped<CreateTeamHandler>();
        services.AddScoped<GetTeamsHandler>();
        services.AddScoped<GetTeamDetailsHandler>();

        return services;
    }
}