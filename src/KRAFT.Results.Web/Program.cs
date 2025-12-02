using KRAFT.Results.Web.Components;
using KRAFT.Results.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<RedirectManager>();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    Uri baseAddress = builder.Configuration.GetValue<Uri>("API:BaseAddress")
        ?? throw new InvalidOperationException("No API base address");

    client.BaseAddress = baseAddress;
});

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();