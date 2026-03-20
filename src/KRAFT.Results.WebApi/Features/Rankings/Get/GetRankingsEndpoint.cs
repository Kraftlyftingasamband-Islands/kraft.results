using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Rankings;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Rankings.Get;

internal static class GetRankingsEndpoint
{
    internal const string Name = "GetRankings";

    internal static RouteGroupBuilder MapGetRankingsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async (
            [FromQuery] string? discipline,
            [FromQuery] int? year,
            [FromQuery] string? equipmentType,
            [FromQuery] string? gender,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] GetRankingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            PagedResponse<RankingEntry> result = await handler.Handle(
                discipline ?? "total",
                year,
                equipmentType,
                gender,
                page ?? 1,
                pageSize ?? 50,
                cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets rankings")
        .WithDescription("Gets a paginated list of rankings filtered by discipline, year, equipment type, and gender")
        .Produces<PagedResponse<RankingEntry>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}