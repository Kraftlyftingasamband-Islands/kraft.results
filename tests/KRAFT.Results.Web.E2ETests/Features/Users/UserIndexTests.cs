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

        await page.GotoAsync($"{_fixture.BaseUrl}/login");
        ILocator usernameInput = page.Locator("#username");
        await usernameInput.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await usernameInput.FillAsync("testuser");
        await page.Locator("#password").FillAsync("testuser");
        await page.Locator("button[type='submit']").ClickAsync();

        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });

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