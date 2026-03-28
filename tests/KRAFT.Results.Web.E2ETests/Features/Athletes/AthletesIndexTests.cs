using Microsoft.Playwright;

using static Microsoft.Playwright.Assertions;

namespace KRAFT.Results.Web.E2ETests.Features.Athletes;

public class AthletesIndexTests(PlaywrightFixture fixture)
{
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task LoadsAthletesPage()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;

        // Act
        await page.GotoAsync($"{_fixture.BaseUrl}/athletes");
        ILocator heading = page.Locator("h3", new PageLocatorOptions { HasText = "Keppendur" });
        await Expect(heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator searchInput = page.Locator("input.search-input");
        await Expect(searchInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Assert
        await Expect(heading).ToHaveTextAsync("Keppendur", new LocatorAssertionsToHaveTextOptions { Timeout = PageConstants.DefaultTimeoutMs });

        await Expect(searchInput).ToBeVisibleAsync();

        ILocator athleteRows = page.Locator("table tbody tr");
        await Expect(athleteRows).Not.ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }

    [Fact]
    public async Task FiltersAthletes_WhenSearchTermEntered()
    {
        // Arrange
        (IBrowserContext context, IPage page) = await _fixture.NewPageAsync();
        await using IAsyncDisposable contextGuard = context;
        await page.GotoAsync($"{_fixture.BaseUrl}/athletes");
        ILocator searchInput = page.Locator("input.search-input");
        await Expect(searchInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        ILocator athleteRows = page.Locator("table tbody tr");
        await Expect(athleteRows.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Wait for the Blazor circuit to finish initializing — AthletesIndex.OnAfterRenderAsync
        // focuses the search input after OnInitializedAsync completes. Filling before this
        // causes the fill to land on the SSR-rendered input; the circuit then re-renders with
        // fresh state (_searchTerm = ""), silently discarding the typed value.
        await Expect(searchInput).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = PageConstants.DefaultTimeoutMs });

        // Act
        await searchInput.FillAsync("zzzznonexistent");

        // Assert
        await Expect(athleteRows).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = PageConstants.DefaultTimeoutMs });
    }
}