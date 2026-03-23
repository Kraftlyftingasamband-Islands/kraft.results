using Microsoft.Playwright;

using Shouldly;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Athletes;

public class AthletesIndexTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsAthletesPage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/athletes");
        ILocator heading = page.Locator("h3", new PageLocatorOptions { HasText = "Keppendur" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator searchInput = page.Locator("input.search-input");
        await searchInput.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string headingText = await heading.InnerTextAsync();
        headingText.ShouldBe("Keppendur");

        bool searchVisible = await searchInput.IsVisibleAsync();
        searchVisible.ShouldBeTrue();

        ILocator athleteRows = page.Locator("table tbody tr");
        await athleteRows.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int rowCount = await athleteRows.CountAsync();
        rowCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task FiltersAthletes_WhenSearchTermEntered()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;
        await page.GotoAsync($"{_fixture.BaseUrl}/athletes");
        ILocator searchInput = page.Locator("input.search-input");
        await searchInput.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator athleteRows = page.Locator("table tbody tr");
        await athleteRows.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        await searchInput.FillAsync("zzzznonexistent");

        // Assert
        await Expect(athleteRows).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }
}