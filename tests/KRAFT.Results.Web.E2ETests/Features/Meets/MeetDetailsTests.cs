using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Meets;

public class MeetDetailsTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsMeetDetails()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act — navigate via meet index to use Blazor enhanced navigation
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{TestDataSeeder.SeededMeetYear}");
        ILocator firstMeetLink = page.Locator("article.meet-item a").First;
        await firstMeetLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await firstMeetLink.ClickAsync();

        // Wait for the meet details page to render
        ILocator resultsHeading = page.Locator("h2", new PageLocatorOptions { HasText = "Úrslit" });
        await resultsHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string resultsText = await resultsHeading.InnerTextAsync();
        resultsText.ShouldBe("Úrslit");
    }
}