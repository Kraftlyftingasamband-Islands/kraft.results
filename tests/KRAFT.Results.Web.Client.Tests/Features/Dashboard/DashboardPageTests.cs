using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Dashboard;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Dashboard;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Dashboard;

public sealed class DashboardPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenRecentAndUpcomingMeetsArePopulated_EachColumnRendersOneMeetCard()
    {
        // Arrange
        DashboardSummary summary = MakeSummary(
            recentMeets: [MakeMeet("recent-meet", "Síðasta mót")],
            upcomingMeets: [MakeMeet("upcoming-meet", "Næsta mót")]);
        RegisterHttpClient(summary);

        // Act
        IRenderedComponent<DashboardPage> cut = _context.Render<DashboardPage>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            IReadOnlyList<AngleSharp.Dom.IElement> cards = cut.FindAll("article.meet-card");
            cards.Count.ShouldBe(2);
        });
    }

    [Fact]
    public void WhenRecentMeetsIsEmpty_RendersRecentEmptyState()
    {
        // Arrange
        DashboardSummary summary = MakeSummary(
            recentMeets: [],
            upcomingMeets: [MakeMeet("upcoming-meet", "Næsta mót")]);
        RegisterHttpClient(summary);

        // Act
        IRenderedComponent<DashboardPage> cut = _context.Render<DashboardPage>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Engin nýleg mót");
        });
    }

    [Fact]
    public void WhenUpcomingMeetsIsEmpty_RendersUpcomingEmptyState()
    {
        // Arrange
        DashboardSummary summary = MakeSummary(
            recentMeets: [MakeMeet("recent-meet", "Síðasta mót")],
            upcomingMeets: []);
        RegisterHttpClient(summary);

        // Act
        IRenderedComponent<DashboardPage> cut = _context.Render<DashboardPage>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Engin mót skráð í dagatal");
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static MeetSummary MakeMeet(string slug, string title) =>
        new(
            Slug: slug,
            Title: title,
            Location: "Reykjavík",
            StartDate: new DateOnly(2024, 3, 15),
            Category: "Kraftlyftingar",
            IsClassic: true,
            IsUpcoming: false,
            ParticipantCount: 5);

    private static DashboardSummary MakeSummary(
        IReadOnlyList<MeetSummary>? recentMeets = null,
        IReadOnlyList<MeetSummary>? upcomingMeets = null) =>
        new(
            SeasonStats: new DashboardSeasonStats(Meets: 0, Athletes: 0, Records: 0, Clubs: 0),
            RecentMeets: recentMeets ?? [],
            UpcomingMeets: upcomingMeets ?? [],
            TopRankingsMen: [],
            TopRankingsWomen: [],
            LatestRecordsMen: [],
            LatestRecordsWomen: [],
            TeamStandingsMen: [],
            TeamStandingsWomen: []);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(DashboardSummary summary)
    {
        DashboardPageMockHandler handler = new(summary);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    private sealed class DashboardPageMockHandler(DashboardSummary summary) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(summary),
            };
            return Task.FromResult(response);
        }
    }
}
