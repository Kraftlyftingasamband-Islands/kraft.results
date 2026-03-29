using KRAFT.Results.WebApi.Features.Users.ChangeRole;
using KRAFT.Results.WebApi.Features.Users.Create;
using KRAFT.Results.WebApi.Features.Users.Delete;
using KRAFT.Results.WebApi.Features.Users.Get;
using KRAFT.Results.WebApi.Features.Users.GetEditDetails;
using KRAFT.Results.WebApi.Features.Users.Infrastructure;
using KRAFT.Results.WebApi.Features.Users.Login;
using KRAFT.Results.WebApi.Features.Users.Update;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserServices
{
    internal static IServiceCollection AddUsers(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<TokenProvider>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<DeleteUserHandler>();
        services.AddScoped<GetUsersHandler>();
        services.AddScoped<GetUserEditDetailsHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<ChangeUserRoleHandler>();

        services.AddOptions<JwtOptions>()
            .Configure<IConfiguration>((options, configuration)
                => configuration.GetRequiredSection(JwtOptions.SectionName).Bind(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}