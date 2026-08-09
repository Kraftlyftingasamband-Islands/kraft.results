using KRAFT.Results.WebApi.Features.Clubs.Create;
using KRAFT.Results.WebApi.Features.Clubs.Delete;
using KRAFT.Results.WebApi.Features.Clubs.Get;
using KRAFT.Results.WebApi.Features.Clubs.GetDetails;
using KRAFT.Results.WebApi.Features.Clubs.GetOptions;
using KRAFT.Results.WebApi.Features.Clubs.Update;

namespace KRAFT.Results.WebApi.Features.Clubs;

internal static class ClubServices
{
    internal static IServiceCollection AddClubs(this IServiceCollection services)
    {
        services.AddScoped<CreateClubHandler>();
        services.AddScoped<GetClubsHandler>();
        services.AddScoped<GetClubOptionsHandler>();
        services.AddScoped<GetClubDetailsHandler>();
        services.AddScoped<UpdateClubHandler>();
        services.AddScoped<DeleteClubHandler>();

        return services;
    }
}
