using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Dashboard;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Dashboard;

[Collection(nameof(InfraCollection))]
public sealed class GetDashboardTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/dashboard";
    private const string MeetsPath = "/meets";

    private readonly HttpClient _client = fixture.Factory!.CreateClient();
    private readonly HttpClient _authorizedClient = fixture.CreateAuthorizedHttpClient();
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private string _recentMeetSlug = string.Empty;
    private string _upcomingMeetSlug = string.Empty;
    private string _attemptlessMeetSlug = string.Empty;
    private int _recentMeetId;
    private int _recentParticipationId;
    private HttpClient _recordClient = null!;
    private RecordComputationChannel _channel = null!;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        (_recordClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

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
        HttpResponseMessage attemptResponse = await _recordClient.PutAsJsonAsync(
            $"{MeetsPath}/{_recentMeetId}/participants/{_recentParticipationId}/attempts/{(int)Discipline.Squat}/1",
            recordAttemptCommand,
            CancellationToken.None);
        attemptResponse.EnsureSuccessStatusCode();
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

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

        foreach ((int meetId, int participationId) in _participations)
        {
            await _authorizedClient.DeleteAsync(
                $"{MeetsPath}/{meetId}/participants/{participationId}", CancellationToken.None);
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

        foreach (string slug in _meetSlugs)
        {
            await _authorizedClient.DeleteAsync($"{MeetsPath}/{slug}", CancellationToken.None);
        }

        _client.Dispose();
        _authorizedClient.Dispose();
        _recordClient.Dispose();
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

    [Fact]
    public async Task WhenAllAttemptsAreNoGood_MeetStillAppearsInRecentMeets()
    {
        // Arrange
        string meetSlug = await CreateMeetSlugAsync(new DateOnly(2021, 3, 1));
        int meetId = await GetMeetIdAsync(meetSlug);
        int participationId = await AddParticipantAsync(meetId);
        _participations.Add((meetId, participationId));

        RecordAttemptCommand noGoodAttempt = new RecordAttemptCommandBuilder()
            .WithWeight(100.0m)
            .WithGood(false)
            .Build();

        HttpResponseMessage attemptResponse = await _recordClient.PutAsJsonAsync(
            $"{MeetsPath}/{meetId}/participants/{participationId}/attempts/{(int)Discipline.Squat}/1",
            noGoodAttempt,
            CancellationToken.None);
        attemptResponse.EnsureSuccessStatusCode();
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Act
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        // Assert
        DashboardSummary dashboard = result.ShouldNotBeNull();
        dashboard.RecentMeets.ShouldContain(m => m.Slug == meetSlug);
    }

    [Fact]
    public async Task LatestRecordsMen_AgeCategoryIsTranslatedToIcelandicLabel()
    {
        // Arrange — seed a masters1 male athlete locally.
        // Born 1975, meet date 2021-01-01: IPF age = 2021 − 1975 = 46 → masters1.
        // The 2021 meet date keeps this meet older than the 2022 range used by
        // WhenMoreThanThreeQualifyingMeetsExist, so it does not displace any of
        // that test's meets from the top-3 recent-meets window.
        string masters1Code = UniqueShortCode.Next();
        string masters1FirstName = $"DashM1{masters1Code}";
        string masters1LastName = "Athlete";
        string masters1AthleteSlug = Slug.Create($"{masters1FirstName} {masters1LastName}");
        string masters1MeetSlug = string.Empty;
        int masters1MeetId = 0;
        int masters1ParticipationId = 0;

        try
        {
            CreateAthleteCommand masters1AthleteCommand = new CreateAthleteCommandBuilder()
                .WithFirstName(masters1FirstName)
                .WithLastName(masters1LastName)
                .WithGender("m")
                .WithDateOfBirth(new DateOnly(1975, 1, 1))
                .Build();

            HttpResponseMessage masters1AthleteResponse = await _authorizedClient.PostAsJsonAsync(
                "/athletes", masters1AthleteCommand, CancellationToken.None);
            masters1AthleteResponse.EnsureSuccessStatusCode();

            CreateMeetCommand masters1MeetCommand = new CreateMeetCommandBuilder()
                .WithStartDate(new DateOnly(2021, 1, 1))
                .WithMeetTypeId(1)
                .WithIsRaw(true)
                .WithPublishedResults(true)
                .WithPublishedInCalendar(false)
                .Build();

            HttpResponseMessage masters1MeetResponse = await _authorizedClient.PostAsJsonAsync(
                MeetsPath, masters1MeetCommand, CancellationToken.None);
            masters1MeetResponse.EnsureSuccessStatusCode();
            masters1MeetSlug = masters1MeetResponse.Headers.Location!.ToString().TrimStart('/');

            MeetDetails? masters1MeetDetails = await _authorizedClient.GetFromJsonAsync<MeetDetails>(
                $"{MeetsPath}/{masters1MeetSlug}", CancellationToken.None);
            masters1MeetId = masters1MeetDetails!.MeetId;

            AddParticipantCommand masters1ParticipantCommand = new AddParticipantCommandBuilder()
                .WithAthleteSlug(masters1AthleteSlug)
                .WithBodyWeight(88.0m)
                .WithAgeCategorySlug("masters1")
                .Build();

            HttpResponseMessage masters1ParticipantResponse = await _recordClient.PostAsJsonAsync(
                $"{MeetsPath}/{masters1MeetId}/participants", masters1ParticipantCommand, CancellationToken.None);
            masters1ParticipantResponse.EnsureSuccessStatusCode();

            AddParticipantResponse? masters1ParticipantResult = await masters1ParticipantResponse.Content
                .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);
            masters1ParticipationId = masters1ParticipantResult!.ParticipationId;

            RecordAttemptCommand masters1AttemptCommand = new RecordAttemptCommandBuilder()
                .WithWeight(185.0m)
                .Build();

            HttpResponseMessage masters1AttemptResponse = await _recordClient.PutAsJsonAsync(
                $"{MeetsPath}/{masters1MeetId}/participants/{masters1ParticipationId}/attempts/{(int)Discipline.Squat}/1",
                masters1AttemptCommand,
                CancellationToken.None);
            masters1AttemptResponse.EnsureSuccessStatusCode();
            await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

            // Act
            DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(
                Path, CancellationToken.None);

            // Assert
            DashboardSummary dashboard = result.ShouldNotBeNull();
            dashboard.LatestRecordsMen.ShouldContain(r => r.AgeCategory == "Öldungaflokkur 1");
        }
        finally
        {
            if (masters1ParticipationId > 0)
            {
                await _authorizedClient.DeleteAsync(
                    $"{MeetsPath}/{masters1MeetId}/participants/{masters1ParticipationId}",
                    CancellationToken.None);
            }

            if (!string.IsNullOrEmpty(masters1MeetSlug))
            {
                await _authorizedClient.DeleteAsync($"{MeetsPath}/{masters1MeetSlug}", CancellationToken.None);
            }

            await _authorizedClient.DeleteAsync($"/athletes/{masters1AthleteSlug}", CancellationToken.None);
        }
    }

    [Fact]
    public async Task WhenMoreThanThreeQualifyingMeetsExist_ReturnsThreeMostRecentByStartDate()
    {
        // Arrange — seed 4 past meets with distinct StartDates, all with attempts, all PublishedResults
        string slug1 = await CreateMeetSlugAsync(new DateOnly(2022, 1, 1));
        string slug2 = await CreateMeetSlugAsync(new DateOnly(2022, 4, 1));
        string slug3 = await CreateMeetSlugAsync(new DateOnly(2022, 7, 1));
        string slug4 = await CreateMeetSlugAsync(new DateOnly(2022, 10, 1));

        await RecordOneAttemptAsync(await GetMeetIdAsync(slug1));
        await RecordOneAttemptAsync(await GetMeetIdAsync(slug2));
        await RecordOneAttemptAsync(await GetMeetIdAsync(slug3));
        await RecordOneAttemptAsync(await GetMeetIdAsync(slug4));

        // Act
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        // Assert — the 3 newest slugs appear; the oldest (slug1) does not
        DashboardSummary dashboard = result.ShouldNotBeNull();
        IReadOnlyList<MeetSummary> recentMeets = dashboard.RecentMeets;
        recentMeets.ShouldContain(m => m.Slug == slug4);
        recentMeets.ShouldContain(m => m.Slug == slug3);
        recentMeets.ShouldContain(m => m.Slug == slug2);
        recentMeets.ShouldNotContain(m => m.Slug == slug1);
        recentMeets
            .Select(m => m.StartDate)
            .ShouldBeInOrder(SortDirection.Descending);
    }

    private async Task<string> CreateMeetSlugAsync(DateOnly startDate)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithMeetTypeId(1)
            .WithIsRaw(true)
            .WithPublishedResults(true)
            .WithPublishedInCalendar(false)
            .Build();

        HttpResponseMessage response = await _authorizedClient.PostAsJsonAsync(MeetsPath, command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);
        return slug;
    }

    private async Task<int> GetMeetIdAsync(string meetSlug)
    {
        MeetDetails? meetDetails = await _authorizedClient.GetFromJsonAsync<MeetDetails>(
            $"{MeetsPath}/{meetSlug}", CancellationToken.None);
        return meetDetails!.MeetId;
    }

    private async Task<int> AddParticipantAsync(int meetId)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder().Build();
        HttpResponseMessage response = await _authorizedClient.PostAsJsonAsync(
            $"{MeetsPath}/{meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);
        return result!.ParticipationId;
    }

    private async Task RecordOneAttemptAsync(int meetId)
    {
        int participationId = await AddParticipantAsync(meetId);
        _participations.Add((meetId, participationId));

        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(100.0m)
            .Build();

        HttpResponseMessage response = await _recordClient.PutAsJsonAsync(
            $"{MeetsPath}/{meetId}/participants/{participationId}/attempts/{(int)Discipline.Squat}/1",
            command,
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);
    }
}
