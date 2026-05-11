using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Dashboard;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Dashboard;

[Collection(nameof(InfraCollection))]
public sealed class GetDashboardTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/dashboard";
    private const string MeetsPath = "/meets";

    private readonly HttpClient _client = fixture.Factory!.CreateClient();
    private readonly HttpClient _authorizedClient = fixture.CreateAuthorizedHttpClient();
    private string _recentMeetSlug = string.Empty;
    private string _upcomingMeetSlug = string.Empty;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        CreateMeetCommand recentCommand = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2020, 1, 1))
            .WithMeetTypeId(1)
            .WithIsRaw(true)
            .WithPublishedResults(true)
            .WithPublishedInCalendar(false)
            .Build();

        HttpResponseMessage recentResponse = await _authorizedClient.PostAsJsonAsync(MeetsPath, recentCommand, CancellationToken.None);
        recentResponse.EnsureSuccessStatusCode();
        _recentMeetSlug = recentResponse.Headers.Location!.ToString().TrimStart('/');

        CreateMeetCommand upcomingCommand = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2099, 6, 1))
            .WithMeetTypeId(1)
            .WithIsRaw(true)
            .WithPublishedResults(false)
            .WithPublishedInCalendar(true)
            .Build();

        HttpResponseMessage upcomingResponse = await _authorizedClient.PostAsJsonAsync(MeetsPath, upcomingCommand, CancellationToken.None);
        upcomingResponse.EnsureSuccessStatusCode();
        _upcomingMeetSlug = upcomingResponse.Headers.Location!.ToString().TrimStart('/');
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_recentMeetSlug))
        {
            await _authorizedClient.DeleteAsync($"{MeetsPath}/{_recentMeetSlug}", CancellationToken.None);
        }

        if (!string.IsNullOrEmpty(_upcomingMeetSlug))
        {
            await _authorizedClient.DeleteAsync($"{MeetsPath}/{_upcomingMeetSlug}", CancellationToken.None);
        }

        _client.Dispose();
        _authorizedClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.RecentMeets.ShouldNotBeNull();
        result.UpcomingMeets.ShouldNotBeNull();
        result.TopRankingsMen.ShouldNotBeNull();
        result.TopRankingsWomen.ShouldNotBeNull();
        result.RecentRecordsMen.ShouldNotBeNull();
        result.RecentRecordsWomen.ShouldNotBeNull();
        result.TeamStandingsMen.ShouldNotBeNull();
        result.TeamStandingsWomen.ShouldNotBeNull();
    }

    [Fact]
    public async Task SeasonStats_AreNonNegative()
    {
        // Arrange

        // Act
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.SeasonStats.Meets.ShouldBeGreaterThanOrEqualTo(0);
        result.SeasonStats.Athletes.ShouldBeGreaterThanOrEqualTo(0);
        result.SeasonStats.Records.ShouldBeGreaterThanOrEqualTo(0);
        result.SeasonStats.Clubs.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RecentMeet_ReturnsDisciplineIsClassicAndParticipantCount()
    {
        // Act
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        // Assert
        DashboardSummary dashboard = result.ShouldNotBeNull();
        dashboard.RecentMeets.ShouldContain(m => m.Slug == _recentMeetSlug);
        MeetSummary meet = dashboard.RecentMeets.First(m => m.Slug == _recentMeetSlug);
        meet.ShouldSatisfyAllConditions(
            () => meet.Discipline.ShouldBe(KRAFT.Results.Contracts.Constants.Powerlifting),
            () => meet.IsClassic.ShouldBeTrue(),
            () => meet.ParticipantCount.ShouldBe(0));
    }

    [Fact]
    public async Task UpcomingMeet_ReturnsDisciplineIsClassicAndParticipantCount()
    {
        // Act
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        // Assert
        DashboardSummary dashboard = result.ShouldNotBeNull();
        dashboard.UpcomingMeets.ShouldContain(m => m.Slug == _upcomingMeetSlug);
        MeetSummary meet = dashboard.UpcomingMeets.First(m => m.Slug == _upcomingMeetSlug);
        meet.ShouldSatisfyAllConditions(
            () => meet.Discipline.ShouldBe(KRAFT.Results.Contracts.Constants.Powerlifting),
            () => meet.IsClassic.ShouldBeTrue(),
            () => meet.ParticipantCount.ShouldBe(0));
    }
}