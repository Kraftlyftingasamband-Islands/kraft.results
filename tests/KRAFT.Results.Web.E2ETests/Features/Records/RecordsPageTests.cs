using Microsoft.Playwright;

using Shouldly;

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
        bool breadcrumbVisible = await breadcrumb.IsVisibleAsync();
        breadcrumbVisible.ShouldBeTrue();

        ILocator recordTables = page.Locator(".record-section table");
        await recordTables.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        int tableCount = await recordTables.CountAsync();
        tableCount.ShouldBeGreaterThan(0);
    }
}