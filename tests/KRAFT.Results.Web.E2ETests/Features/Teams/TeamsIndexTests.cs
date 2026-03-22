using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Teams;

public class TeamsIndexTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsTeamsPage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/teams");
        ILocator heading = page.Locator("h3", new PageLocatorOptions { HasText = "Félög" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string headingText = await heading.InnerTextAsync();
        headingText.ShouldBe("Félög");

        ILocator teamNames = page.Locator(".card-grid .team-name");
        int teamCount = await teamNames.CountAsync();
        teamCount.ShouldBeGreaterThan(0);
    }
}