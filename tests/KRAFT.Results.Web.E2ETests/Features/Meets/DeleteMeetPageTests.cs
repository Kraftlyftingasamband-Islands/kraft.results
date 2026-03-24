using Microsoft.Playwright;

using Shouldly;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Meets;

public class DeleteMeetPageTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task ShowsDeleteButton_WhenLoggedInAsAdmin()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        string slug = await CreateMeetViaUi(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{slug}");
        ILocator deleteButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Eyða" });
        await deleteButton.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(deleteButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task DoesNotShowDeleteButton_WhenNotLoggedIn()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act — navigate via the index to ensure Blazor enhanced navigation works
        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{TestDataSeeder.SeededMeetYear}");
        ILocator firstMeetLink = page.Locator("article.meet-item a").First;
        await firstMeetLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await firstMeetLink.ClickAsync();

        ILocator resultsHeading = page.Locator("h2", new PageLocatorOptions { HasText = "Úrslit" });
        await resultsHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        ILocator deleteButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Eyða" });
        await Expect(deleteButton).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ShowsConfirmDialog_WhenDeleteButtonClicked()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        string slug = await CreateMeetViaUi(page);

        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{slug}");
        ILocator deleteButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Eyða" });
        await deleteButton.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        await deleteButton.ClickAsync();

        // Assert
        ILocator dialog = page.Locator("dialog[open]");
        await Expect(dialog).ToBeVisibleAsync();

        ILocator confirmButton = dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Eyða móti" });
        await Expect(confirmButton).ToBeVisibleAsync();

        ILocator cancelButton = dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Hætta við" });
        await Expect(cancelButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ClosesDialog_WhenCancelClicked()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        string slug = await CreateMeetViaUi(page);

        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{slug}");
        ILocator deleteButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Eyða" });
        await deleteButton.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await deleteButton.ClickAsync();
        ILocator dialog = page.Locator("dialog[open]");
        await Expect(dialog).ToBeVisibleAsync();

        // Act
        ILocator cancelButton = dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Hætta við" });
        await cancelButton.ClickAsync();

        // Assert
        await Expect(dialog).ToBeHiddenAsync();
    }

    [Fact]
    public async Task DeletesMeetAndNavigatesToIndex_WhenConfirmed()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        string slug = await CreateMeetViaUi(page);

        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{slug}");
        ILocator deleteButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Eyða" });
        await deleteButton.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await deleteButton.ClickAsync();
        ILocator dialog = page.Locator("dialog[open]");
        await Expect(dialog).ToBeVisibleAsync();

        // Act
        ILocator confirmButton = dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Eyða móti" });
        await confirmButton.ClickAsync();

        // Assert — should navigate to /meets
        await page.WaitForURLAsync("**/meets", new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string url = page.Url;
        url.ShouldEndWith("/meets");
    }

    [Fact]
    public async Task ShowsError_WhenMeetHasParticipations()
    {
        // Arrange — the seeded meet has participations; navigate via index for reliable loading
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        await page.GotoAsync($"{_fixture.BaseUrl}/meets/{TestDataSeeder.SeededMeetYear}");
        ILocator firstMeetLink = page.Locator("article.meet-item a").First;
        await firstMeetLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await firstMeetLink.ClickAsync();

        ILocator resultsHeading = page.Locator("h2", new PageLocatorOptions { HasText = "Úrslit" });
        await resultsHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator deleteButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Eyða" });
        await deleteButton.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        await deleteButton.ClickAsync();

        ILocator dialog = page.Locator("dialog[open]");
        await dialog.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator confirmButton = dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Eyða móti" });
        await confirmButton.ClickAsync();

        // Assert — should show error message in the dialog
        ILocator errorMessage = page.Locator(".confirm-dialog-error");
        await errorMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        string errorText = await errorMessage.InnerTextAsync();
        errorText.ShouldContain("keppendur");
    }

    private async Task<string> CreateMeetViaUi(IPage page)
    {
        string meetTitle = $"E2E Delete Meet {Guid.NewGuid():N}";

        await page.GotoAsync($"{_fixture.BaseUrl}/meets/create");
        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt mót" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await page.Locator("#title").FillAsync(meetTitle);
        await page.Locator("#start-date").FillAsync("2026-08-15");
        await page.Locator("#meet-type").SelectOptionAsync(new SelectOptionValue { Label = "Kraftlyftingar" });
        await page.Locator("button[type='submit']").ClickAsync();

        await page.WaitForURLAsync("**/meets", new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Derive slug from title - the API creates slug from "{title} {year}"
        string normalizedTitle = meetTitle.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);
        return $"{normalizedTitle}-2026";
    }
}