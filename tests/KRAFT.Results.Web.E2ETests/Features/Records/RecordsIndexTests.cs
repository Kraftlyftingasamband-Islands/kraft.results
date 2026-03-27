using Microsoft.Playwright;

using Shouldly;

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
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string headingText = await heading.InnerTextAsync();
        headingText.ShouldBe("Met");

        ILocator equipmentToggle = page.Locator(".equipment-toggle");
        await equipmentToggle.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(equipmentToggle).ToBeVisibleAsync();

        ILocator maleSection = page.Locator("h2", new PageLocatorOptions { HasText = "Karlar" });
        string maleText = await maleSection.InnerTextAsync();
        maleText.ShouldBe("Karlar");

        ILocator femaleSection = page.Locator("h2", new PageLocatorOptions { HasText = "Konur" });
        string femaleText = await femaleSection.InnerTextAsync();
        femaleText.ShouldBe("Konur");

        ILocator categoryCards = page.Locator(".category-grid .card-link");
        await Expect(categoryCards.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int cardCount = await categoryCards.CountAsync();
        cardCount.ShouldBeGreaterThan(0);
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
        await eraSelector.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(eraSelector).ToBeVisibleAsync();

        ILocator buttons = eraSelector.Locator("button");
        await Expect(buttons).ToHaveCountAsync(2, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator activeButton = eraSelector.Locator("button.active");
        await Expect(activeButton).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });

        string activeText = await activeButton.InnerTextAsync();
        activeText.ShouldContain("Current Era");
    }

    [Fact]
    public async Task CategoryLinks_IncludeEraParam_WhenHistoricalEraSelected()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await page.GotoAsync($"{_fixture.BaseUrl}/records");
        ILocator eraSelector = page.Locator(".era-selector");
        await eraSelector.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        ILocator historicalButton = eraSelector.Locator("button", new LocatorLocatorOptions { HasText = "Historical Era" });
        await historicalButton.ClickAsync();

        // Assert
        ILocator categoryLinks = page.Locator(".category-grid .card-link");
        await Expect(categoryLinks.First).ToHaveAttributeAsync(
            "href",
            new System.Text.RegularExpressions.Regex("era=historical-era"),
            new LocatorAssertionsToHaveAttributeOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int linkCount = await categoryLinks.CountAsync();
        linkCount.ShouldBeGreaterThan(0);

        for (int i = 0; i < linkCount; i++)
        {
            string? href = await categoryLinks.Nth(i).GetAttributeAsync("href");
            href.ShouldNotBeNull();
            href.ShouldContain("era=historical-era");
        }
    }
}