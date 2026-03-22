using Microsoft.Playwright;

using Shouldly;

namespace KRAFT.Results.Web.E2ETests.Features.Athletes;

public class AthleteDetailsTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsAthleteDetails()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/athletes");
        ILocator firstAthleteLink = page.Locator("table tbody tr .nav-link").First;
        await firstAthleteLink.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });
        await firstAthleteLink.ClickAsync();

        ILocator heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        string name = await heading.InnerTextAsync();
        name.ShouldNotBeNullOrWhiteSpace();

        ILocator athleteMeta = page.Locator(".athlete-meta");
        string meta = await athleteMeta.InnerTextAsync();
        meta.ShouldContain("Fæðingarár:");
    }
}