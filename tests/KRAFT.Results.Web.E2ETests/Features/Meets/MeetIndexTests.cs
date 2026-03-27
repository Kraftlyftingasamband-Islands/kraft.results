using System.Globalization;

using Microsoft.Playwright;

using Shouldly;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Meets;

public class MeetIndexTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsMeetIndex_WithCurrentYear()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;
        int currentYear = DateTime.Now.Year;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/meets");
        ILocator yearValue = page.Locator(".year-value");
        await yearValue.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string yearText = await yearValue.InnerTextAsync();
        yearText.ShouldBe(currentYear.ToString(CultureInfo.InvariantCulture));

        ILocator yearLabel = page.Locator(".year-label");
        string label = await yearLabel.InnerTextAsync();
        label.ShouldBe("MÓTASKRÁ", StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task LoadsMeetIndex_WithSpecificYear()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{TestDataSeeder.SeededMeetYear}");
        ILocator yearValue = page.Locator(".year-value");
        await yearValue.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string yearText = await yearValue.InnerTextAsync();
        yearText.ShouldBe(TestDataSeeder.SeededMeetYear.ToString(CultureInfo.InvariantCulture));

        ILocator meetItems = page.Locator("article.meet-item");
        await Expect(meetItems.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int meetCount = await meetItems.CountAsync();
        meetCount.ShouldBeGreaterThan(0);
    }
}