using KRAFT.Results.WebApi.Features.Records.Get;
using KRAFT.Results.WebApi.Features.Records.GetHistory;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordEndpoints
{
    internal static IEndpointRouteBuilder MapRecordEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/records")
            .WithTags("Records");

        group.MapGetRecordsEndpoint();
        group.MapGetRecordHistoryEndpoint();

        return group;
    }
}