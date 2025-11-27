using KRAFT.Results.WebApi.Features.Meets.Create;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetServices
{
    internal static IServiceCollection AddMeets(this IServiceCollection services)
    {
        services.AddTransient<CreateMeetHandler>();

        return services;
    }
}