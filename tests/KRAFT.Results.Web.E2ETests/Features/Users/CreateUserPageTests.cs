using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Users;

public class CreateUserPageTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task DisplaysCreateUserForm_WhenLoggedInAsAdmin()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await page.GotoAsync($"{_fixture.BaseUrl}/login");
        ILocator usernameInput = page.Locator("#username");
        await usernameInput.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator submitButton = page.Locator("button[type='submit']");
        await submitButton.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await page.WaitForFunctionAsync(
            "() => !document.querySelector('button[type=\"submit\"]').disabled",
            null,
            new PageWaitForFunctionOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await usernameInput.FillAsync("testuser");
        await page.Locator("#password").FillAsync("testuser");
        await submitButton.ClickAsync();

        await page.WaitForFunctionAsync(
            "() => !window.location.href.toLowerCase().includes('/login')",
            null,
            new PageWaitForFunctionOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/users/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýr notandi" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        bool headingVisible = await heading.IsVisibleAsync();
        headingVisible.ShouldBeTrue();
    }
}