using KRAFT.Results.Contracts.Eras;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Eras.Get;

internal static class GetErasEndpoint
{
    internal const string Name = "GetEras";

    internal static RouteGroupBuilder MapGetErasEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async Task<Ok<List<EraSummary>>> (
            [FromServices] GetErasHandler handler,
            CancellationToken cancellationToken) =>
        {
            List<EraSummary> eras = await handler.Handle(cancellationToken);

            return TypedResults.Ok(eras);
        })
        .WithName(Name)
        .WithSummary("Gets all eras")
        .WithDescription("Gets all eras ordered by start date")
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}