using System.Globalization;

using KRAFT.Results.Web.Client.Features.Auth;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("is-IS");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("is-IS");

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthServices();

await builder.Build().RunAsync();