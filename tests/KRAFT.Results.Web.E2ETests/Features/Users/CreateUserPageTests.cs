using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

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

        await _fixture.LoginAsync(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/users/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýr notandi" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).ToBeVisibleAsync();
    }
}