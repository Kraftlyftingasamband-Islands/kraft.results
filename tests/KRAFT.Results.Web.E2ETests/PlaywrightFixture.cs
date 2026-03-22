using System.Diagnostics.CodeAnalysis;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(KRAFT.Results.Web.E2ETests.PlaywrightFixture))]

namespace KRAFT.Results.Web.E2ETests;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "BaseUrl is used for string concatenation in tests")]
    public string BaseUrl { get; private set; } = string.Empty;

    public async Task<(IBrowserContext Context, IPage Page)> NewPageAsync()
    {
        IBrowser browser = _browser ?? throw new InvalidOperationException("Browser not initialized.");
        IBrowserContext context = await browser.NewContextAsync();
        IPage page = await context.NewPageAsync();
        return (context, page);
    }

    public async ValueTask InitializeAsync()
    {
        IDistributedApplicationTestingBuilder builder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.KRAFT_Results_AppHost>();

        builder.Configuration["SqlServer:DataVolume"] = "kraft-data-e2etest";

        _app = await builder.BuildAsync();

        await _app.StartAsync();

        Aspire.Hosting.ApplicationModel.ResourceNotificationService notificationService =
            _app.Services.GetRequiredService<Aspire.Hosting.ApplicationModel.ResourceNotificationService>();
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
        try
        {
            await notificationService.WaitForResourceHealthyAsync("web", cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                "The 'web' resource did not become healthy within 2 minutes. " +
                "Verify that Docker is running and the AppHost can start all resources.");
        }

        BaseUrl = _app.GetEndpoint("web").ToString().TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        if (_playwright is not null)
        {
            _playwright.Dispose();
        }

        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}