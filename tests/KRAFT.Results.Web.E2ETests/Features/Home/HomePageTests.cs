using System.Text.RegularExpressions;

using Microsoft.Playwright;

using Shouldly;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Home;

public class HomePageTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsHomePage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        IResponse? response = await page.GotoAsync(_fixture.BaseUrl);

        // Assert
        response.ShouldNotBeNull();
        response.Status.ShouldBe(200);

        ILocator nav = page.Locator("nav.nav");
        await Expect(nav).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await Expect(page).ToHaveTitleAsync(
            new Regex("KRAFT Results", RegexOptions.IgnoreCase),
            new PageAssertionsToHaveTitleOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    [Fact]
    public async Task ShowsNavigation()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync(_fixture.BaseUrl);
        ILocator nav = page.Locator("nav.nav");
        await Expect(nav).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(nav).ToContainTextAsync("Forsíða", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(nav).ToContainTextAsync("Mótaskrá", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(nav).ToContainTextAsync("Keppendur", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(nav).ToContainTextAsync("Stigatöflur", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(nav).ToContainTextAsync("Met", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(nav).ToContainTextAsync("Félög", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Login link moved to top bar on desktop
        ILocator loginLink = page.Locator(".top-bar").GetByText("Innskrá");
        await Expect(loginLink).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }
}