using KRAFT.Results.WebApi.Features.Users.Create;
using KRAFT.Results.WebApi.Features.Users.Get;
using KRAFT.Results.WebApi.Features.Users.Login;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserEndpoints
{
    internal static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/users")
            .WithTags("Users");

        group.MapLoginEndpoint();
        group.MapCreateUserEndpoint();
        group.MapGetUsersEndpoint();

        return endpoints;
    }
}