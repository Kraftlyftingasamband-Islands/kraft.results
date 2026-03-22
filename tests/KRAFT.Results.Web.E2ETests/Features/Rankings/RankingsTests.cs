using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Rankings;

public class RankingsTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsRankingsPage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/rankings");
        ILocator heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string headingText = await heading.InnerTextAsync();
        headingText.ShouldBe("Stigatöflur");

        ILocator filterBar = page.Locator(".filter-bar");
        bool filterVisible = await filterBar.IsVisibleAsync();
        filterVisible.ShouldBeTrue();
    }
}