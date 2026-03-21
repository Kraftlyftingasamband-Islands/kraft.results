using KRAFT.Results.WebApi.Features.Records.Get;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordEndpoints
{
    internal static IEndpointRouteBuilder MapRecordEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/records")
            .WithTags("Records");

        group.MapGetRecordsEndpoint();

        return group;
    }
}