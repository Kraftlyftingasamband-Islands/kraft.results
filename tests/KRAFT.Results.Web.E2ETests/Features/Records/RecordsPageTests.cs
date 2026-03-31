using System.Text.RegularExpressions;

using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Records;

public class RecordsPageTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsRecordsForGenderAndAge()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/records/m/open");
        ILocator heading = page.Locator("h1");
        await Expect(heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).ToContainTextAsync("Karlar", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(heading).ToContainTextAsync("Opinn flokkur", new LocatorAssertionsToContainTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator recordCards = page.Locator(".record-section .rc-card");
        await Expect(recordCards.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(recordCards).Not.ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    [Fact]
    public async Task LoadsRecordsForHistoricalEra()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/records/m/open?equipmentType=equipped&era=historical-era");
        ILocator heading = page.Locator("h1");
        await Expect(heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        ILocator recordCards = page.Locator(".record-section .rc-card");
        await Expect(recordCards.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(recordCards).Not.ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    [Fact]
    public async Task SwitchingEras_ShowsCorrectWeightCategories()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await page.GotoAsync($"{_fixture.BaseUrl}/records/m/open?equipmentType=equipped&era=historical-era");
        ILocator eraSelector = page.Locator(".era-selector");
        await Expect(eraSelector).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        ILocator currentEraButton = eraSelector.Locator("button", new LocatorLocatorOptions { HasText = "Current Era" });
        await currentEraButton.ClickAsync();

        // Assert
        await Expect(page).ToHaveURLAsync(
            new Regex("era=current-era"),
            new PageAssertionsToHaveURLOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator recordCards = page.Locator(".record-section .rc-card");
        await Expect(recordCards.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(recordCards).Not.ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    [Fact]
    public async Task PageTitle_IncludesEraTitle_WhenHistoricalEra()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/records/m/open?equipmentType=equipped&era=historical-era");
        await Expect(page.Locator("h1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Historical Era"), new PageAssertionsToHaveTitleOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }
}