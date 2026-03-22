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
        await page.GotoAsync($"{_fixture.BaseUrl}/teams");
        ILocator firstTeamLink = page.Locator(".card-grid a[aria-label]").First;
        await firstTeamLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs, State = WaitForSelectorState.Attached });
        string? href = await firstTeamLink.GetAttributeAsync("href");
        href.ShouldNotBeNullOrWhiteSpace();
        await page.GotoAsync($"{_fixture.BaseUrl}{href}");

        ILocator heading = page.Locator("h3").First;
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string teamTitle = await heading.InnerTextAsync();
        teamTitle.ShouldNotBeNullOrWhiteSpace();

        ILocator membersHeading = page.Locator("h4", new PageLocatorOptions { HasText = "Meðlimir" });
        await membersHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string membersText = await membersHeading.InnerTextAsync();
        membersText.ShouldBe("Meðlimir");

        ILocator memberRows = page.Locator("table tr");
        int memberCount = await memberRows.CountAsync();
        memberCount.ShouldBeGreaterThan(0);
    }
}