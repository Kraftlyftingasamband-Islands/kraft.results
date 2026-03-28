using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Athletes;

public class AthleteDetailsTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsAthleteDetails()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/athletes/testie-mctestface");

        ILocator heading = page.Locator("h1").First;
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).Not.ToBeEmptyAsync(new LocatorAssertionsToBeEmptyOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator athleteMeta = page.Locator(".athlete-meta");
        await Expect(athleteMeta).ToContainTextAsync("Fæðingarár:");
    }
}