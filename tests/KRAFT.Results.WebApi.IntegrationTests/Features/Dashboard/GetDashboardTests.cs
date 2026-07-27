using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
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
    private string _attemptlessMeetSlug = string.Empty;
    private int _recentMeetId;
    private int _recentParticipationId;

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

        MeetDetails? meetDetails = await _authorizedClient.GetFromJsonAsync<MeetDetails>(
            $"{MeetsPath}/{_recentMeetSlug}", CancellationToken.None);
        _recentMeetId = meetDetails!.MeetId;

        AddParticipantCommand addParticipantCommand = new AddParticipantCommandBuilder().Build();
        HttpResponseMessage participantResponse = await _authorizedClient.PostAsJsonAsync(
            $"{MeetsPath}/{_recentMeetId}/participants", addParticipantCommand, CancellationToken.None);
        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);
        _recentParticipationId = participantResult!.ParticipationId;

        RecordAttemptCommand recordAttemptCommand = new RecordAttemptCommandBuilder()
            .WithWeight(100.0m)
            .Build();
        HttpResponseMessage attemptResponse = await _authorizedClient.PutAsJsonAsync(
            $"{MeetsPath}/{_recentMeetId}/participants/{_recentParticipationId}/attempts/{(int)Discipline.Squat}/1",
            recordAttemptCommand,
            CancellationToken.None);
        attemptResponse.EnsureSuccessStatusCode();

        CreateMeetCommand attemptlessCommand = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2020, 2, 1))
            .WithMeetTypeId(1)
            .WithIsRaw(true)
            .WithPublishedResults(true)
            .WithPublishedInCalendar(false)
            .Build();

        HttpResponseMessage attemptlessResponse = await _authorizedClient.PostAsJsonAsync(MeetsPath, attemptlessCommand, CancellationToken.None);
        attemptlessResponse.EnsureSuccessStatusCode();
        _attemptlessMeetSlug = attemptlessResponse.Headers.Location!.ToString().TrimStart('/');

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
        if (_recentParticipationId > 0)
        {
            await _authorizedClient.DeleteAsync(
                $"{MeetsPath}/{_recentMeetId}/participants/{_recentParticipationId}", CancellationToken.None);
        }

        if (!string.IsNullOrEmpty(_recentMeetSlug))
        {
            await _authorizedClient.DeleteAsync($"{MeetsPath}/{_recentMeetSlug}", CancellationToken.None);
        }

        if (!string.IsNullOrEmpty(_attemptlessMeetSlug))
        {
            await _authorizedClient.DeleteAsync($"{MeetsPath}/{_attemptlessMeetSlug}", CancellationToken.None);
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
        result.LatestRecordsMen.ShouldNotBeNull();
        result.LatestRecordsWomen.ShouldNotBeNull();
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
            () => meet.ParticipantCount.ShouldBe(1));
    }

    [Fact]
    public async Task WhenRecentMeetHasNoAttempts_ExcludedFromRecentMeets()
    {
        // Arrange

        // Act
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        // Assert
        DashboardSummary dashboard = result.ShouldNotBeNull();
        dashboard.RecentMeets.ShouldNotContain(m => m.Slug == _attemptlessMeetSlug);
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
