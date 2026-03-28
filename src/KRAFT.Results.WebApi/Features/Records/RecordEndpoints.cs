using KRAFT.Results.WebApi.Features.Records.Approve;
using KRAFT.Results.WebApi.Features.Records.Get;
using KRAFT.Results.WebApi.Features.Records.GetHistory;
using KRAFT.Results.WebApi.Features.Records.Reject;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordEndpoints
{
    internal static IEndpointRouteBuilder MapRecordEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/records")
            .WithTags("Records");

        group.MapGetRecordsEndpoint();
        group.MapGetRecordHistoryEndpoint();
        group.MapApproveRecordEndpoint();
        group.MapRejectRecordEndpoint();

        return group;
    }
}