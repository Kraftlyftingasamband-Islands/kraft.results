using KRAFT.Results.Web.Client.Features.Auth;
using KRAFT.Results.Web.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAuthServices();
builder.Services.AddCascadingAuthenticationState();

Uri apiBaseAddress = builder.Configuration.GetValue<Uri>("API:BaseAddress")
    ?? throw new InvalidOperationException("No API base address");

builder.Services.AddScoped(services =>
{
    TokenStorageService tokenStorage = services.GetRequiredService<TokenStorageService>();

    HttpClientHandler httpHandler = new();
    if (!builder.Environment.IsProduction())
    {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
        httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830
    }

    AuthorizationMessageHandler handler = new(tokenStorage, apiBaseAddress)
    {
        InnerHandler = httpHandler,
    };

    return new HttpClient(handler)
    {
        BaseAddress = apiBaseAddress,
    };
});

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