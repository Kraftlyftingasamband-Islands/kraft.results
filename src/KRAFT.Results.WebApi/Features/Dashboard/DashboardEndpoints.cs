using KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

namespace KRAFT.Results.WebApi.Features.Dashboard;

internal static class DashboardEndpoints
{
    internal static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/dashboard")
            .WithTags("Dashboard");

        group.MapGetDashboardEndpoint();

        return group;
    }
}