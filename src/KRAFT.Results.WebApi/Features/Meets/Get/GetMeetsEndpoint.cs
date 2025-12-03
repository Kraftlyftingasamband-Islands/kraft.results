using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.Get;

internal static class GetMeetsEndpoint
{
    internal const string Name = "GetMeets";

    internal static RouteGroupBuilder MapGetMeetsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async (
            [FromQuery] int? year,
            [FromServices] GetMeetsHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<MeetSummary> result = await handler.Handle(year, cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets meets")
        .WithDescription("Gets a list of meets")
        .Produces<IReadOnlyList<MeetSummary>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}