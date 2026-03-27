using Microsoft.Playwright;

using Shouldly;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Meets;

public class CreateMeetPageTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task DisplaysCreateMeetForm_WhenLoggedInAsAdmin()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt mót" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task DisplaysAllFormFields()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt mót" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        ILocator nafnLabel = page.GetByText("Nafn", new PageGetByTextOptions { Exact = true });
        await Expect(nafnLabel).ToBeVisibleAsync();

        ILocator dagsetningLabel = page.GetByText("Dagsetning", new PageGetByTextOptions { Exact = true });
        await Expect(dagsetningLabel).ToBeVisibleAsync();

        ILocator tegundLabel = page.GetByText("Tegund móts", new PageGetByTextOptions { Exact = true });
        await Expect(tegundLabel).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreatesMeet_WhenFormSubmittedWithValidData()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt mót" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        await page.Locator("#title").FillAsync($"E2E Test Meet {Guid.NewGuid():N}");
        await page.Locator("#start-date").FillAsync("2026-06-15");
        await page.Locator("#meet-type").SelectOptionAsync(new SelectOptionValue { Label = "Kraftlyftingar" });
        await page.Locator("button[type='submit']").ClickAsync();

        // Assert — should navigate back to /meets on success
        await page.WaitForURLAsync("**/meets", new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string url = page.Url;
        url.ShouldEndWith("/meets");
    }

    [Fact]
    public async Task ShowsDuplicateError_WhenMeetAlreadyExists()
    {
        // Arrange — create a meet first, then try to create the same one
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        string meetTitle = $"E2E Duplicate Meet {Guid.NewGuid():N}";

        // Create the first meet
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");
        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt mót" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await page.Locator("#title").FillAsync(meetTitle);
        await page.Locator("#start-date").FillAsync("2026-07-01");
        await page.Locator("#meet-type").SelectOptionAsync(new SelectOptionValue { Label = "Kraftlyftingar" });
        await page.Locator("button[type='submit']").ClickAsync();

        await page.WaitForURLAsync("**/meets", new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string firstCreateUrl = page.Url;
        firstCreateUrl.ShouldEndWith("/meets");

        // Act — try to create the same meet again
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");
        heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt mót" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await page.Locator("#title").FillAsync(meetTitle);
        await page.Locator("#start-date").FillAsync("2026-07-01");
        await page.Locator("#meet-type").SelectOptionAsync(new SelectOptionValue { Label = "Kraftlyftingar" });
        await page.Locator("button[type='submit']").ClickAsync();

        // Assert
        ILocator errorMessage = page.Locator("#meet-form-error");
        await errorMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string errorText = await errorMessage.InnerTextAsync();
        errorText.ShouldBe("Mót með þessu nafni og dagsetningu er þegar til.");
    }

    [Fact]
    public async Task ShowsValidationErrors_WhenRequiredFieldsMissing()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt mót" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act — click submit without filling name or date (meet type is auto-selected)
        await page.Locator("button[type='submit']").ClickAsync();

        // Assert — should show validation messages
        ILocator validationMessages = page.Locator(".validation-message");
        await Expect(validationMessages.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        int messageCount = await validationMessages.CountAsync();
        messageCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task RedirectsToLogin_WhenNotAuthenticated()
    {
        // Arrange — do NOT login
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");

        // Assert — should redirect to login
        await page.WaitForURLAsync("**/login**", new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string url = page.Url;
        url.ShouldContain("/login");
    }
}