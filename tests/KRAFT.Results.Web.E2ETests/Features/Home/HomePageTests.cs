using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Home;

public class HomePageTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsHomePage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        IResponse? response = await page.GotoAsync(_fixture.BaseUrl);

        // Assert
        response.ShouldNotBeNull();
        response.Status.ShouldBe(200);

        ILocator nav = page.Locator("nav.nav");
        await nav.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        string title = await page.TitleAsync();
        title.ShouldContain("KRAFT Results");
    }

    [Fact]
    public async Task ShowsNavigation()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync(_fixture.BaseUrl);
        ILocator nav = page.Locator("nav.nav");
        await nav.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        ILocator loginLink = nav.GetByText("Innskrá");
        await loginLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        string navText = await nav.InnerTextAsync();
        navText.ShouldContain("Forsíða");
        navText.ShouldContain("Mótaskrá");
        navText.ShouldContain("Keppendur");
        navText.ShouldContain("Stigatöflur");
        navText.ShouldContain("Met");
        navText.ShouldContain("Félög");
        navText.ShouldContain("Innskrá");
    }
}