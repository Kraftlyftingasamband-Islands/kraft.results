using Microsoft.Playwright;

using Shouldly;

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
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string headingText = await heading.InnerTextAsync();
        headingText.ShouldBe("KRAFT.Results");

        ILocator usernameInput = page.Locator("#username");
        bool usernameVisible = await usernameInput.IsVisibleAsync();
        usernameVisible.ShouldBeTrue();

        ILocator passwordInput = page.Locator("#password");
        bool passwordVisible = await passwordInput.IsVisibleAsync();
        passwordVisible.ShouldBeTrue();

        ILocator submitButton = page.Locator("button[type='submit']");
        bool submitVisible = await submitButton.IsVisibleAsync();
        submitVisible.ShouldBeTrue();

        string submitText = await submitButton.InnerTextAsync();
        submitText.ShouldContain("Innskrá");
    }
}