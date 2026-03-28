using KRAFT.Results.WebApi.Features.Users.Create;
using KRAFT.Results.WebApi.Features.Users.Delete;
using KRAFT.Results.WebApi.Features.Users.Get;
using KRAFT.Results.WebApi.Features.Users.GetEditDetails;
using KRAFT.Results.WebApi.Features.Users.Login;
using KRAFT.Results.WebApi.Features.Users.Update;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserEndpoints
{
    internal static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/users")
            .WithTags("Users");

        group.MapLoginEndpoint();
        group.MapCreateUserEndpoint();
        group.MapDeleteUserEndpoint();
        group.MapGetUsersEndpoint();
        group.MapGetUserEditDetailsEndpoint();
        group.MapUpdateUserEndpoint();

        return endpoints;
    }
}