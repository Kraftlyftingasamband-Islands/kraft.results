using KRAFT.Results.WebApi.Features.Countries.Get;

namespace KRAFT.Results.WebApi.Features.Countries;

internal static class CountryServices
{
    internal static IServiceCollection AddCountries(this IServiceCollection services)
    {
        services.AddScoped<GetCountriesHandler>();

        return services;
    }
}