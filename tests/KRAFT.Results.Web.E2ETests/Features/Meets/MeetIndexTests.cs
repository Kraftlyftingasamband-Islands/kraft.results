using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.Playwright;

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
        await Expect(yearValue).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(yearValue).ToHaveTextAsync(
            currentYear.ToString(CultureInfo.InvariantCulture),
            new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator yearLabel = page.Locator(".year-label");
        await Expect(yearLabel).ToHaveTextAsync(
            new Regex("MÓTASKRÁ", RegexOptions.IgnoreCase),
            new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
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
        await Expect(yearValue).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(yearValue).ToHaveTextAsync(
            TestDataSeeder.SeededMeetYear.ToString(CultureInfo.InvariantCulture),
            new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator meetItems = page.Locator("article.meet-item");
        await Expect(meetItems.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await Expect(meetItems).Not.ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }
}