using KRAFT.Results.WebApi.Features.TeamCompetition.Get;

namespace KRAFT.Results.WebApi.Features.TeamCompetition;

internal static class TeamCompetitionServices
{
    internal static IServiceCollection AddTeamCompetition(this IServiceCollection services)
    {
        services.AddScoped<GetTeamCompetitionHandler>();

        return services;
    }
}