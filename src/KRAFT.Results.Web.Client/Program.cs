using KRAFT.Results.Web.Client.Features.Auth;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthServices();

await builder.Build().RunAsync();