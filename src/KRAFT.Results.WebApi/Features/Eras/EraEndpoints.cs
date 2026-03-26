using KRAFT.Results.WebApi.Features.Eras.Get;

namespace KRAFT.Results.WebApi.Features.Eras;

internal static class EraEndpoints
{
    internal static IEndpointRouteBuilder MapEraEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/eras")
            .WithTags("Eras");

        group.MapGetErasEndpoint();

        return group;
    }
}