using System.Text.RegularExpressions;

using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Auth;

public class LoginTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsLoginPage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/login");
        ILocator heading = page.Locator("h1.login-title");
        await Expect(heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).ToHaveTextAsync("KRAFT.Results", new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator usernameInput = page.Locator("#username");
        await Expect(usernameInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator passwordInput = page.Locator("#password");
        await Expect(passwordInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator submitButton = page.Locator("button[type='submit']");
        await Expect(submitButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await Expect(submitButton).ToHaveTextAsync(new Regex("Innskrá"), new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }
}