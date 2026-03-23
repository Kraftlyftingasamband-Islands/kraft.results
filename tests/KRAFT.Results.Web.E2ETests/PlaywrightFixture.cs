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
        IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        IPage page = await context.NewPageAsync();
        return (context, page);
    }

    public async Task LoginAsync(IPage page)
    {
        await page.GotoAsync($"{BaseUrl}/login");
        ILocator usernameInput = page.Locator("#username");
        await usernameInput.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await page.WaitForFunctionAsync(
            "() => window.Blazor !== undefined && window.Blazor !== null",
            null,
            new PageWaitForFunctionOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await usernameInput.FillAsync("testuser");
        await page.Locator("#password").FillAsync("testuser");
        await page.Locator("button[type='submit']").ClickAsync();

        await page.WaitForFunctionAsync(
            "() => !window.location.href.toLowerCase().includes('/login')",
            null,
            new PageWaitForFunctionOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    public async ValueTask InitializeAsync()
    {
        string[] args =
        [
            "--Parameters:sql-password=E2eTest_Password1!",
            "--SqlServer:DataVolume=kraft-data-e2etest",
            "--SqlServer:ContainerName=kraft-sql-e2etest",
            "--SqlServer:Port=11433",
            "--SqlServer:Persistent=false",
        ];

        IDistributedApplicationTestingBuilder builder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.KRAFT_Results_AppHost>(args);

        _app = await builder.BuildAsync();

        await _app.StartAsync();

        Aspire.Hosting.ApplicationModel.ResourceNotificationService notificationService =
            _app.Services.GetRequiredService<Aspire.Hosting.ApplicationModel.ResourceNotificationService>();
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
        try
        {
            await notificationService.WaitForResourceHealthyAsync("api", cts.Token);
            await notificationService.WaitForResourceHealthyAsync("web", cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                "The 'api' or 'web' resource did not become healthy within 2 minutes. " +
                "Verify that Docker is running and the AppHost can start all resources.");
        }

        string? connectionString = await _app.GetConnectionStringAsync("kraft-db");
        if (connectionString is not null)
        {
            if (!connectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += ";TrustServerCertificate=True";
            }

            await TestDataSeeder.SeedAsync(connectionString);
        }

        BaseUrl = _app.GetEndpoint("web").ToString().TrimEnd('/');

        using HttpClientHandler warmupHandler = new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

#pragma warning disable CA5400 // Cert revocation check not needed in E2E tests
        using HttpClient warmupClient = new(warmupHandler, disposeHandler: false);
#pragma warning restore CA5400

        using CancellationTokenSource warmupCts = new(TimeSpan.FromSeconds(30));
        while (!warmupCts.Token.IsCancellationRequested)
        {
            try
            {
                HttpResponseMessage response = await warmupClient.GetAsync(BaseUrl, warmupCts.Token);
                string body = await response.Content.ReadAsStringAsync(warmupCts.Token);
                if (response.IsSuccessStatusCode && body.Contains("KRAFT", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
            catch (HttpRequestException)
            {
                // App not ready yet
            }

            await Task.Delay(500, warmupCts.Token);
        }

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