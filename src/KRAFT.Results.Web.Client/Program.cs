using KRAFT.Results.Web.Client.Features.Auth;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthServices();

Uri baseAddress = new(builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped(services =>
{
    TokenStorageService tokenStorage = services.GetRequiredService<TokenStorageService>();
    AuthorizationMessageHandler handler = new(tokenStorage, baseAddress)
    {
        InnerHandler = new HttpClientHandler(),
    };

    return new HttpClient(handler)
    {
        BaseAddress = baseAddress,
    };
});

await builder.Build().RunAsync();