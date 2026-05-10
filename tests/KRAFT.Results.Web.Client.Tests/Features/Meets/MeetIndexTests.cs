using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Meets;

public sealed class MeetIndexTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient([], delay: true);

        // Act
        IRenderedComponent<MeetIndex> cut = _context.Render<MeetIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki mót...");
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<MeetIndex> cut = _context.Render<MeetIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsMeetItems_WhenMeetsAreLoaded()
    {
        // Arrange
        List<MeetSummary> meets =
        [
            new("nationals-2024", "Nationals 2024", "Reykjavik", new DateOnly(2024, 3, 15)),
            new("spring-cup-2024", "Spring Cup 2024", "Akureyri", new DateOnly(2024, 4, 20)),
        ];
        RegisterHttpClient(meets);

        // Act
        IRenderedComponent<MeetIndex> cut = _context.Render<MeetIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".meet-item").Count.ShouldBe(2);
        });
    }

    [Fact]
    public void YearNavRemainsVisible_WhileLoading()
    {
        // Arrange
        RegisterHttpClient([], delay: true);

        // Act
        IRenderedComponent<MeetIndex> cut = _context.Render<MeetIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert — YearNav is outside DataLoader and always renders
        cut.Find(".year-nav").ShouldNotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(List<MeetSummary> meets, bool delay = false)
    {
        MockHttpMessageHandler<MeetSummary> handler = new(meets, delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterFailingHttpClient()
    {
        FailingHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }
}