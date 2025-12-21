using KRAFT.Results.Web.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient(
    "WebApi",
    client =>
    {
        Uri baseAddress = builder.Configuration.GetValue<Uri>("API:BaseAddress")
            ?? throw new InvalidOperationException("No API base address");

        client.BaseAddress = baseAddress;
    });

builder.Services.AddScoped(services => services.GetRequiredService<IHttpClientFactory>()
    .CreateClient("WebApi"));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(KRAFT.Results.Web.Client._Imports).Assembly);

await app.RunAsync();