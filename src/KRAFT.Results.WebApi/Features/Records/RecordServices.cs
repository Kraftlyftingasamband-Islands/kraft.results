using KRAFT.Results.WebApi.Features.Records.Approve;
using KRAFT.Results.WebApi.Features.Records.DetectRecords;
using KRAFT.Results.WebApi.Features.Records.Get;
using KRAFT.Results.WebApi.Features.Records.GetHistory;
using KRAFT.Results.WebApi.Features.Records.Reject;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordServices
{
    internal static IServiceCollection AddRecords(this IServiceCollection services)
    {
        services.AddScoped<GetRecordsHandler>();
        services.AddScoped<GetRecordHistoryHandler>();
        services.AddScoped<ApproveRecordHandler>();
        services.AddScoped<RejectRecordHandler>();
        services.AddScoped<RecordDetectionService>();

        return services;
    }
}