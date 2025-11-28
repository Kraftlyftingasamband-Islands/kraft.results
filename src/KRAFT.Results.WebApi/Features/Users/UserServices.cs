using KRAFT.Results.WebApi.Features.Users.Create;
using KRAFT.Results.WebApi.Features.Users.Infrastructure;
using KRAFT.Results.WebApi.Features.Users.Login;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserServices
{
    internal static IServiceCollection AddUsers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<LoginHandler>();
        services.AddTransient<TokenProvider>();
        services.AddTransient<CreateUserHandler>();

        // Configure JWT options from appsettings
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        return services;
    }
}