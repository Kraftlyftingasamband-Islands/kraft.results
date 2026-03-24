using System.Text.RegularExpressions;

using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Athletes;

public class AthleteDetailsTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsAthleteDetails()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/athletes");
        ILocator firstAthleteLink = page.Locator("table tbody tr .nav-link").First;
        await firstAthleteLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await firstAthleteLink.ClickAsync();

        await page.WaitForURLAsync(new Regex(@"/athletes/[a-z0-9-]+$"), new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator athleteMeta = page.Locator(".athlete-meta");
        await athleteMeta.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        ILocator heading = page.Locator("h1");
        await Expect(heading).ToBeVisibleAsync();
        await Expect(heading).Not.ToBeEmptyAsync();

        await Expect(athleteMeta).ToContainTextAsync("Fæðingarár:");
    }
}