using KRAFT.Results.Contracts.Clubs;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Clubs.Get;

internal static class GetClubsEndpoint
{
    internal const string Name = "GetClubs";

    internal static RouteGroupBuilder MapGetClubsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async (
            [FromServices] GetClubsHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<ClubSummary> result = await handler.Handle(cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets clubs")
        .WithDescription("Gets a list of all clubs")
        .Produces<IReadOnlyList<ClubSummary>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
