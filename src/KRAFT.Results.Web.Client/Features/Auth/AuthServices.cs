using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace KRAFT.Results.Web.Client.Features.Auth;

public static class AuthServices
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddScoped<TokenStorageService>();
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<JwtAuthenticationStateProvider>());

        return services;
    }
}