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
}