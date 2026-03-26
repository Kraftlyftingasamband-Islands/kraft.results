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
        ILocator eraToggle = page.Locator(".era-toggle");
        await eraToggle.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(eraToggle).ToBeVisibleAsync();

        ILocator buttons = eraToggle.Locator("button");
        int buttonCount = await buttons.CountAsync();
        buttonCount.ShouldBe(2);

        ILocator activeButton = eraToggle.Locator("button.active");
        int activeCount = await activeButton.CountAsync();
        activeCount.ShouldBe(1);

        string activeText = await activeButton.InnerTextAsync();
        activeText.ShouldBe("Current Era");
    }

    [Fact]
    public async Task CategoryLinks_IncludeEraParam_WhenHistoricalEraSelected()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await page.GotoAsync($"{_fixture.BaseUrl}/records");
        ILocator eraToggle = page.Locator(".era-toggle");
        await eraToggle.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        ILocator historicalButton = eraToggle.Locator("button", new LocatorLocatorOptions { HasText = "Historical Era" });
        await historicalButton.ClickAsync();

        // Assert
        ILocator categoryLinks = page.Locator(".category-grid .card-link");
        await categoryLinks.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
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