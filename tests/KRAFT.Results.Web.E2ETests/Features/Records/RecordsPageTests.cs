using Microsoft.Playwright;

using Shouldly;

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
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string headingText = await heading.InnerTextAsync();
        headingText.ShouldContain("Karlar");
        headingText.ShouldContain("open");

        ILocator breadcrumb = page.Locator("nav.breadcrumb");
        await breadcrumb.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(breadcrumb).ToBeVisibleAsync();

        ILocator recordTables = page.Locator(".record-section table");
        await recordTables.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int tableCount = await recordTables.CountAsync();
        tableCount.ShouldBeGreaterThan(0);
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
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        ILocator recordTables = page.Locator(".record-section table");
        await recordTables.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int tableCount = await recordTables.CountAsync();
        tableCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SwitchingEras_ShowsCorrectWeightCategories()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await page.GotoAsync($"{_fixture.BaseUrl}/records/m/open?equipmentType=equipped&era=historical-era");
        ILocator eraToggle = page.Locator(".era-toggle");
        await eraToggle.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        ILocator currentEraButton = eraToggle.Locator("button", new LocatorLocatorOptions { HasText = "Current Era" });
        await currentEraButton.ClickAsync();

        // Assert
        await page.WaitForFunctionAsync(
            "() => window.location.href.includes('era=current-era')",
            null,
            new PageWaitForFunctionOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string url = page.Url;
        url.ShouldContain("era=current-era");

        ILocator recordTables = page.Locator(".record-section table");
        await recordTables.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int tableCount = await recordTables.CountAsync();
        tableCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Breadcrumb_IncludesEraTitle_WhenHistoricalEra()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/records/m/open?equipmentType=equipped&era=historical-era");
        ILocator breadcrumb = page.Locator("nav.breadcrumb");
        await breadcrumb.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string breadcrumbText = await breadcrumb.InnerTextAsync();
        breadcrumbText.ShouldContain("Historical Era");
    }
}