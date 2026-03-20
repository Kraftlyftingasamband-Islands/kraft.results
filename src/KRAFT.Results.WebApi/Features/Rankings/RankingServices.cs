using KRAFT.Results.WebApi.Features.Rankings.Get;

namespace KRAFT.Results.WebApi.Features.Rankings;

internal static class RankingServices
{
    internal static IServiceCollection AddRankings(this IServiceCollection services)
    {
        services.AddScoped<GetRankingsHandler>();

        return services;
    }
}