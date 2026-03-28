using KRAFT.Results.WebApi.Features.Records.Get;
using KRAFT.Results.WebApi.Features.Records.GetHistory;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordServices
{
    internal static IServiceCollection AddRecords(this IServiceCollection services)
    {
        services.AddScoped<GetRecordsHandler>();
        services.AddScoped<GetRecordHistoryHandler>();

        return services;
    }
}