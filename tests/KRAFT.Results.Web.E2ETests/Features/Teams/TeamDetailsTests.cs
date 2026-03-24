using Microsoft.Playwright;

using Shouldly;

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
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string teamTitle = await heading.InnerTextAsync();
        teamTitle.ShouldNotBeNullOrWhiteSpace();

        ILocator membersHeading = page.Locator("h2", new PageLocatorOptions { HasText = "Meðlimir" });
        await membersHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string membersText = await membersHeading.InnerTextAsync();
        membersText.ShouldBe("Meðlimir");
    }
}