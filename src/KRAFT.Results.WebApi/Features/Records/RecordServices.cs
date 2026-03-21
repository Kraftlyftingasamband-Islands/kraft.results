using KRAFT.Results.WebApi.Features.Records.Get;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordServices
{
    internal static IServiceCollection AddRecords(this IServiceCollection services)
    {
        services.AddScoped<GetRecordsHandler>();

        return services;
    }
}