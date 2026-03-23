using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Users;

public class UserIndexTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task DisplaysUserList_WhenLoggedIn()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/users");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Notendur" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        bool headingVisible = await heading.IsVisibleAsync();
        headingVisible.ShouldBeTrue();

        ILocator tableRows = page.Locator("table tbody tr");
        await tableRows.First.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        int rowCount = await tableRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);
    }
}