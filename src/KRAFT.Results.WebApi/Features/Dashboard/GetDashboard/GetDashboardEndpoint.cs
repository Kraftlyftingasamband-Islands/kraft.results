using KRAFT.Results.Contracts.Dashboard;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

internal static class GetDashboardEndpoint
{
    internal static RouteGroupBuilder MapGetDashboardEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async Task<Ok<DashboardSummary>> (
            [FromServices] GetDashboardHandler handler,
            CancellationToken cancellationToken) =>
        {
            DashboardSummary summary = await handler.Handle(cancellationToken);
            return TypedResults.Ok(summary);
        })
        .WithName("GetDashboard")
        .Produces<DashboardSummary>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}