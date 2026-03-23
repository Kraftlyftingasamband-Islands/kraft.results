using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Teams;

public class CreateTeamPageTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task DisplaysCreateTeamForm_WhenLoggedInAsAdmin()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/teams/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt félag" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        bool headingVisible = await heading.IsVisibleAsync();
        headingVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task DisplaysAllFormFields()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/teams/create");

        ILocator heading = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Nýtt félag" });
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        ILocator nafnLabel = page.GetByText("Nafn", new PageGetByTextOptions { Exact = true });
        bool nafnVisible = await nafnLabel.IsVisibleAsync();
        nafnVisible.ShouldBeTrue();

        ILocator skammstofunLabel = page.GetByText("Skammstöfun", new PageGetByTextOptions { Exact = true });
        bool skammstofunVisible = await skammstofunLabel.IsVisibleAsync();
        skammstofunVisible.ShouldBeTrue();

        ILocator fulltNafnLabel = page.GetByText("Fullt nafn", new PageGetByTextOptions { Exact = true });
        bool fulltNafnVisible = await fulltNafnLabel.IsVisibleAsync();
        fulltNafnVisible.ShouldBeTrue();

        ILocator landLabel = page.GetByText("Land", new PageGetByTextOptions { Exact = true });
        bool landVisible = await landLabel.IsVisibleAsync();
        landVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task DisplaysCreateTeamButton_OnTeamsIndex_WhenLoggedInAsAdmin()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/teams");

        ILocator createTeamLink = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Stofna félag" });
        await createTeamLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        bool linkVisible = await createTeamLink.IsVisibleAsync();
        linkVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task NavigatesToCreateTeamPage_WhenButtonClicked()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        await _fixture.LoginAsync(page);

        await page.GotoAsync($"{_fixture.BaseUrl}/teams");

        ILocator createTeamLink = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Stofna félag" });
        await createTeamLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        await createTeamLink.ClickAsync();

        await page.WaitForURLAsync("**/teams/create", new PageWaitForURLOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string url = page.Url;
        url.ShouldEndWith("/teams/create");
    }
}