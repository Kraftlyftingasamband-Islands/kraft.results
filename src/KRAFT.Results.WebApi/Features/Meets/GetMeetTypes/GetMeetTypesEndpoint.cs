using KRAFT.Results.Contracts.Meets;

using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;

internal static class GetMeetTypesEndpoint
{
    internal const string Name = "GetMeetTypes";

    internal static RouteGroupBuilder MapGetMeetTypesEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/types", static async (
            [FromServices] GetMeetTypesHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<MeetTypeSummary> result = await handler.Handle(cancellationToken);

            return result;
        })
        .WithName(Name)
        .WithSummary("Gets meet types")
        .WithDescription("Gets a list of all meet types")
        .Produces<IReadOnlyList<MeetTypeSummary>>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}