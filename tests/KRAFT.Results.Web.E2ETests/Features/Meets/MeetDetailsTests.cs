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

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/2024");
        ILocator firstMeetLink = page.Locator("article.meet-item .nav-link").First;
        await firstMeetLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await firstMeetLink.ClickAsync();

        ILocator heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string title = await heading.InnerTextAsync();
        title.ShouldNotBeNullOrWhiteSpace();

        ILocator resultsHeading = page.Locator("h2", new PageLocatorOptions { HasText = "Úrslit" });
        await resultsHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string resultsText = await resultsHeading.InnerTextAsync();
        resultsText.ShouldBe("Úrslit");
    }
}