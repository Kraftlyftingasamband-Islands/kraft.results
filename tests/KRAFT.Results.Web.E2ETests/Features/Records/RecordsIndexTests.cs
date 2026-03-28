using System.Text.RegularExpressions;

using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Records;

public class RecordsIndexTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsRecordsPage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/records");
        ILocator heading = page.Locator("h1");
        await Expect(heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).ToHaveTextAsync("Met", new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator equipmentToggle = page.Locator(".equipment-toggle");
        await Expect(equipmentToggle).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator maleSection = page.Locator("h2", new PageLocatorOptions { HasText = "Karlar" });
        await Expect(maleSection).ToHaveTextAsync("Karlar", new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator femaleSection = page.Locator("h2", new PageLocatorOptions { HasText = "Konur" });
        await Expect(femaleSection).ToHaveTextAsync("Konur", new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator categoryCards = page.Locator(".category-grid .card-link");
        await Expect(categoryCards.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(categoryCards).Not.ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    [Fact]
    public async Task EraSelector_IsVisible_WhenMultipleErasExist()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/records");
        ILocator eraSelector = page.Locator(".era-selector");
        await Expect(eraSelector).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(eraSelector).ToBeVisibleAsync();

        ILocator buttons = eraSelector.Locator("button");
        await Expect(buttons).ToHaveCountAsync(2, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator activeButton = eraSelector.Locator("button.active");
        await Expect(activeButton).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await Expect(activeButton).ToHaveTextAsync(
            new Regex("Current Era"),
            new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    [Fact]
    public async Task CategoryLinks_IncludeEraParam_WhenHistoricalEraSelected()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await page.GotoAsync($"{_fixture.BaseUrl}/records");
        ILocator eraSelector = page.Locator(".era-selector");
        await Expect(eraSelector).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        ILocator historicalButton = eraSelector.Locator("button", new LocatorLocatorOptions { HasText = "Historical Era" });
        await historicalButton.ClickAsync();

        // Assert
        ILocator categoryLinks = page.Locator(".category-grid .card-link");
        await Expect(categoryLinks.First).ToHaveAttributeAsync(
            "href",
            new Regex("era=historical-era"),
            new LocatorAssertionsToHaveAttributeOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(categoryLinks).Not.ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });

        int linkCount = await categoryLinks.CountAsync();
        for (int i = 0; i < linkCount; i++)
        {
            await Expect(categoryLinks.Nth(i)).ToHaveAttributeAsync(
                "href",
                new Regex("era=historical-era"),
                new LocatorAssertionsToHaveAttributeOptions { Timeout = PageConstants.DefaultTimeoutMs });
        }
    }
}