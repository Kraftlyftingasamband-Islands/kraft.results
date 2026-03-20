using Bunit;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthletesIndexTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingStateInitially()
    {
        // Arrange
        RegisterHttpClient([], delay: true);

        // Act
        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".spinner").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki keppendur...");
    }

    [Fact]
    public void RendersSearchInput()
    {
        // Arrange
        RegisterHttpClient([]);

        // Act
        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Assert
        cut.Find("input[type='search']").ShouldNotBeNull();
    }

    [Fact]
    public void SearchInputHasPlaceholder()
    {
        // Arrange
        RegisterHttpClient([]);

        // Act
        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Assert
        string placeholder = cut.Find("input[type='search']").GetAttribute("placeholder") ?? string.Empty;
        placeholder.ShouldNotBeEmpty();
    }

    [Fact]
    public void HidesSearchInputWhileLoading()
    {
        // Arrange
        RegisterHttpClient([], delay: true);

        // Act
        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Assert
        cut.FindAll("input[type='search']").Count.ShouldBe(0);
    }

    [Fact]
    public void HidesTableWhenNoAthletes()
    {
        // Arrange
        RegisterHttpClient([]);

        // Act
        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Assert
        cut.FindAll("table").Count.ShouldBe(0);
    }

    [Fact]
    public void FiltersAthletesByFirstName()
    {
        // Arrange
        List<AthleteSummary> athletes =
        [
            new("jon-jonsson", "Jon Jonsson", 1990),
            new("anna-karlsdottir", "Anna Karlsdottir", 1985),
            new("gudrun-sigurdardottir", "Gudrun Sigurdardottir", 1992),
        ];
        RegisterHttpClient(athletes);

        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Act
        cut.Find("input[type='search']").Input("Jon");

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> visibleNames = cut.FindAll("table tbody tr td a")
                .Select(e => e.TextContent.Trim())
                .ToList();

            visibleNames.Count.ShouldBe(1);
            visibleNames.ShouldContain(name => name.Contains("Jon Jonsson"));
        });
    }

    [Fact]
    public void FiltersAthletesByLastName()
    {
        // Arrange
        List<AthleteSummary> athletes =
        [
            new("jon-jonsson", "Jon Jonsson", 1990),
            new("anna-karlsdottir", "Anna Karlsdottir", 1985),
        ];
        RegisterHttpClient(athletes);

        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Act
        cut.Find("input[type='search']").Input("Karlsdottir");

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> visibleNames = cut.FindAll("table tbody tr td a")
                .Select(e => e.TextContent.Trim())
                .ToList();

            visibleNames.Count.ShouldBe(1);
            visibleNames.ShouldContain(name => name.Contains("Anna Karlsdottir"));
        });
    }

    [Fact]
    public void SearchIsCaseInsensitive()
    {
        // Arrange
        List<AthleteSummary> athletes =
        [
            new("jon-jonsson", "Jon Jonsson", 1990),
            new("anna-karlsdottir", "Anna Karlsdottir", 1985),
        ];
        RegisterHttpClient(athletes);

        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Act
        cut.Find("input[type='search']").Input("jon");

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> visibleNames = cut.FindAll("table tbody tr td a")
                .Select(e => e.TextContent.Trim())
                .ToList();

            visibleNames.Count.ShouldBe(1);
            visibleNames.ShouldContain(name => name.Contains("Jon Jonsson"));
        });
    }

    [Fact]
    public void SearchSupportsPartialMatch()
    {
        // Arrange
        List<AthleteSummary> athletes =
        [
            new("jon-jonsson", "Jon Jonsson", 1990),
            new("jonas-karlsson", "Jonas Karlsson", 1985),
            new("anna-karlsdottir", "Anna Karlsdottir", 1992),
        ];
        RegisterHttpClient(athletes);

        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Act
        cut.Find("input[type='search']").Input("Jon");

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> visibleNames = cut.FindAll("table tbody tr td a")
                .Select(e => e.TextContent.Trim())
                .ToList();

            visibleNames.Count.ShouldBe(2);
        });
    }

    [Fact]
    public void ClearingSearchShowsAllAthletes()
    {
        // Arrange
        List<AthleteSummary> athletes =
        [
            new("jon-jonsson", "Jon Jonsson", 1990),
            new("anna-karlsdottir", "Anna Karlsdottir", 1985),
        ];
        RegisterHttpClient(athletes);

        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();
        cut.Find("input[type='search']").Input("Jon");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("table tbody tr").Count.ShouldBe(1);
        });

        // Act
        cut.Find("input[type='search']").Input(string.Empty);

        // Assert
        cut.WaitForAssertion(() =>
        {
            int visibleCount = cut.FindAll("table tbody tr").Count;
            visibleCount.ShouldBe(2);
        });
    }

    [Fact]
    public void ShowsEmptyStateWhenNoAthletesMatchSearch()
    {
        // Arrange
        List<AthleteSummary> athletes =
        [
            new("jon-jonsson", "Jon Jonsson", 1990),
        ];
        RegisterHttpClient(athletes);

        IRenderedComponent<AthletesIndex> cut = _context.Render<AthletesIndex>();

        // Act
        cut.Find("input[type='search']").Input("nonexistent");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("table").Count.ShouldBe(0);
            cut.Find(".empty-state").ShouldNotBeNull();
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(List<AthleteSummary> athletes, bool delay = false)
    {
        MockHttpMessageHandler handler = new(athletes, delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
    }
}