using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Teams;

public class TeamDetailsTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsTeamDetails()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/teams/test-team");

        ILocator heading = page.Locator("h1").First;
        await Expect(heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).Not.ToBeEmptyAsync(new LocatorAssertionsToBeEmptyOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator membersHeading = page.Locator("h2", new PageLocatorOptions { HasText = "Meðlimir" });
        await Expect(membersHeading).ToHaveTextAsync("Meðlimir", new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }
}