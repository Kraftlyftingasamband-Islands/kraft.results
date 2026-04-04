using KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

namespace KRAFT.Results.WebApi.Features.Dashboard;

internal static class DashboardServices
{
    internal static IServiceCollection AddDashboard(this IServiceCollection services)
    {
        services.AddScoped<GetDashboardHandler>();
        return services;
    }
}