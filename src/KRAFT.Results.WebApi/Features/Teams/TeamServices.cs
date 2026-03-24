using KRAFT.Results.WebApi.Features.Teams.Create;
using KRAFT.Results.WebApi.Features.Teams.Delete;
using KRAFT.Results.WebApi.Features.Teams.Get;
using KRAFT.Results.WebApi.Features.Teams.GetDetails;
using KRAFT.Results.WebApi.Features.Teams.GetOptions;
using KRAFT.Results.WebApi.Features.Teams.Update;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamServices
{
    internal static IServiceCollection AddTeams(this IServiceCollection services)
    {
        services.AddScoped<CreateTeamHandler>();
        services.AddScoped<GetTeamsHandler>();
        services.AddScoped<GetTeamOptionsHandler>();
        services.AddScoped<GetTeamDetailsHandler>();
        services.AddScoped<UpdateTeamHandler>();
        services.AddScoped<DeleteTeamHandler>();

        return services;
    }
}