using KRAFT.Results.WebApi.Features.Countries.Get;

namespace KRAFT.Results.WebApi.Features.Countries;

internal static class CountryEndpoints
{
    internal static IEndpointRouteBuilder MapCountryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/countries")
            .WithTags("Countries");

        group.MapGetCountriesEndpoint();

        return group;
    }
}