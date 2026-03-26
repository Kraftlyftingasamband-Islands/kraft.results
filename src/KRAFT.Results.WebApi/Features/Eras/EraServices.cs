using KRAFT.Results.WebApi.Features.Eras.Get;

namespace KRAFT.Results.WebApi.Features.Eras;

internal static class EraServices
{
    internal static IServiceCollection AddEras(this IServiceCollection services)
    {
        services.AddScoped<GetErasHandler>();

        return services;
    }
}