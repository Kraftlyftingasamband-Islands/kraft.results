using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.Web.Client.Features.TeamCompetition;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.TeamCompetition;

public sealed class TeamCompetitionIndexTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(MakeResponse(), delay: true);

        // Act
        IRenderedComponent<TeamCompetitionIndex> cut = _context.Render<TeamCompetitionIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki gögn...");
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<TeamCompetitionIndex> cut = _context.Render<TeamCompetitionIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsStandings_WhenDataIsLoaded()
    {
        // Arrange
        TeamCompetitionResponse response = MakeResponse(combined:
        [
            new(1, "Þór", "Þór", "thor", null, 100),
            new(2, "KR", "KR", "kr", null, 80),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<TeamCompetitionIndex> cut = _context.Render<TeamCompetitionIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".standings-list li").Count.ShouldBe(2);
        });
    }

    [Fact]
    public void YearNavRemainsVisible_WhileLoading()
    {
        // Arrange
        RegisterHttpClient(MakeResponse(), delay: true);

        // Act
        IRenderedComponent<TeamCompetitionIndex> cut = _context.Render<TeamCompetitionIndex>(
            parameters => parameters.Add(p => p.Year, 2024));

        // Assert — YearNav is outside DataLoader and always renders
        cut.Find(".year-nav").ShouldNotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static TeamCompetitionResponse MakeResponse(
        IReadOnlyList<TeamCompetitionStanding>? combined = null,
        IReadOnlyList<TeamCompetitionStanding>? women = null,
        IReadOnlyList<TeamCompetitionStanding>? men = null) =>
        new(
            Year: 2024,
            IsGenderSplit: false,
            Women: women ?? [],
            Men: men ?? [],
            Combined: combined ?? []);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(TeamCompetitionResponse response, bool delay = false)
    {
        TeamCompetitionMockHandler handler = new(response, delay);
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

    private sealed class TeamCompetitionMockHandler(TeamCompetitionResponse response, bool delay = false) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response),
            };
        }
    }
}