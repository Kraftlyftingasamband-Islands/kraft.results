using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetTeamPointsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string BasePath = "/meets";

    // 2025 meet — men (body weight 82.0m): Place 1→12pts, Place 2→9pts, Place 3→8pts
    private const decimal AlphaMaleSquat = 100.0m;
    private const decimal AlphaMaleBench = 100.0m;
    private const decimal AlphaMaleDeadlift = 100.0m;

    private const decimal NoTeamSquat = 90.0m;
    private const decimal NoTeamBench = 80.0m;
    private const decimal NoTeamDeadlift = 80.0m;

    private const decimal BetaMaleSquat = 70.0m;
    private const decimal BetaMaleBench = 60.0m;
    private const decimal BetaMaleDeadlift = 70.0m;

    // 2025 meet — women (body weight 57.0m): Place 1→12pts, Place 2→9pts
    private const decimal AlphaFemaleSquat = 90.0m;
    private const decimal AlphaFemaleBench = 70.0m;
    private const decimal AlphaFemaleDeadlift = 100.0m;

    private const decimal BetaFemaleSquat = 80.0m;
    private const decimal BetaFemaleBench = 60.0m;
    private const decimal BetaFemaleDeadlift = 80.0m;

    private const int ExpectedBestN2026TotalPoints = 42; // best 5 of 6: 12+9+8+7+6

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly string _alphaShortCode = UniqueShortCode.Next();
    private readonly string _betaShortCode = UniqueShortCode.Next();
    private readonly string _gammaShortCode = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<string> _teamSlugs = [];

    private string _alphaTeamName = string.Empty;
    private string _betaTeamName = string.Empty;
    private string _gammaTeamSlug = string.Empty;
    private int _alphaTeamId;
    private int _betaTeamId;
    private int _gammaTeamId;

    private string _tc2025MeetSlug = string.Empty;
    private string _tc2026MeetSlug = string.Empty;
    private string _noParticipationsMeetSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // Create teams
        _alphaTeamName = $"Alpha{_suffix}";
        _alphaTeamId = await CreateTeamAsync(_alphaTeamName, _alphaShortCode);

        _betaTeamName = $"Beta{_suffix}";
        _betaTeamId = await CreateTeamAsync(_betaTeamName, _betaShortCode);

        string gammaTeamName = $"Gamma{_suffix}";
        _gammaTeamSlug = Slug.Create(gammaTeamName);
        _gammaTeamId = await CreateTeamAsync(gammaTeamName, _gammaShortCode);

        // Create 3 meets
        int tc2025MeetId = await CreateMeetAndGetIdAsync(new DateOnly(2025, 6, 1));
        _tc2025MeetSlug = _meetSlugs[^1];

        int tc2026MeetId = await CreateMeetAndGetIdAsync(new DateOnly(2026, 6, 1));
        _tc2026MeetSlug = _meetSlugs[^1];

        _noParticipationsMeetSlug = await CreateMeetSlugAsync(new DateOnly(2025, 3, 1));

        // Create athletes for 2025 meet (7 unique athletes)
        string alphaMaleSlug = await CreateAthleteAsync("AlphaM", "m");
        string betaMaleSlug = await CreateAthleteAsync("BetaM", "m");
        string alphaFemaleSlug = await CreateAthleteAsync("AlphaF", "f");
        string betaFemaleSlug = await CreateAthleteAsync("BetaF", "f");
        string dqAlphaMaleSlug = await CreateAthleteAsync("DqAlphaM", "m");
        string noTeamSlug = await CreateAthleteAsync("NoTeam", "m");
        string zeroPtsSlug = await CreateAthleteAsync("ZeroPts", "m");

        // Add participations for 2025 meet
        int alphaMalePid = await AddParticipantAsync(tc2025MeetId, alphaMaleSlug, _alphaTeamId);
        int betaMalePid = await AddParticipantAsync(tc2025MeetId, betaMaleSlug, _betaTeamId);
        int alphaFemalePid = await AddParticipantAsync(tc2025MeetId, alphaFemaleSlug, _alphaTeamId, 57.0m);
        int betaFemalePid = await AddParticipantAsync(tc2025MeetId, betaFemaleSlug, _betaTeamId, 57.0m);
        int dqAlphaMalePid = await AddParticipantAsync(tc2025MeetId, dqAlphaMaleSlug, _alphaTeamId);
        int noTeamPid = await AddParticipantAsync(tc2025MeetId, noTeamSlug, teamId: null);
        await AddParticipantAsync(tc2025MeetId, zeroPtsSlug, _gammaTeamId);

        // DQ the dqAlphaMale participant via 3 failed squat attempts → Place=0, TeamPoints=0
        await RecordAttemptAsync(tc2025MeetId, dqAlphaMalePid, Discipline.Squat, 1, 100.0m, good: false);
        await RecordAttemptAsync(tc2025MeetId, dqAlphaMalePid, Discipline.Squat, 2, 100.0m, good: false);
        await RecordAttemptAsync(tc2025MeetId, dqAlphaMalePid, Discipline.Squat, 3, 100.0m, good: false);

        // Record attempts for 2025 men (same weight category, 82.0m body weight)
        // alphaMale: Total=300 → Place 1 → TeamPoints=12
        await RecordFullTotalAsync(tc2025MeetId, alphaMalePid, AlphaMaleSquat, AlphaMaleBench, AlphaMaleDeadlift);

        // noTeam: Total=250 → Place 2 → TeamPoints=9 (no team, excluded from standings)
        await RecordFullTotalAsync(tc2025MeetId, noTeamPid, NoTeamSquat, NoTeamBench, NoTeamDeadlift);

        // betaMale: Total=200 → Place 3 → TeamPoints=8
        await RecordFullTotalAsync(tc2025MeetId, betaMalePid, BetaMaleSquat, BetaMaleBench, BetaMaleDeadlift);

        // zeroPts: no attempts → Total=0, TeamPoints=null → excluded from standings (Gamma hidden)

        // Record attempts for 2025 women (same weight category, 57.0m body weight)
        // alphaFemale: Total=260 → Place 1 → TeamPoints=12
        await RecordFullTotalAsync(tc2025MeetId, alphaFemalePid, AlphaFemaleSquat, AlphaFemaleBench, AlphaFemaleDeadlift);

        // betaFemale: Total=220 → Place 2 → TeamPoints=9
        await RecordFullTotalAsync(tc2025MeetId, betaFemalePid, BetaFemaleSquat, BetaFemaleBench, BetaFemaleDeadlift);

        // Create 6 male athletes for 2026 meet with descending totals → places 1–6 → points 12,9,8,7,6,5
        // Best 5 of 6 points = 12+9+8+7+6 = 42 (ExpectedBestN2026TotalPoints)
        string alpha26M1Slug = await CreateAthleteAsync("Alpha26M1", "m");
        int alpha26M1Pid = await AddParticipantAsync(tc2026MeetId, alpha26M1Slug, _alphaTeamId);
        await RecordFullTotalAsync(tc2026MeetId, alpha26M1Pid, 200.0m, 150.0m, 250.0m); // Total=600 → Place 1 → 12pts

        string alpha26M2Slug = await CreateAthleteAsync("Alpha26M2", "m");
        int alpha26M2Pid = await AddParticipantAsync(tc2026MeetId, alpha26M2Slug, _alphaTeamId);
        await RecordFullTotalAsync(tc2026MeetId, alpha26M2Pid, 190.0m, 140.0m, 220.0m); // Total=550 → Place 2 → 9pts

        string alpha26M3Slug = await CreateAthleteAsync("Alpha26M3", "m");
        int alpha26M3Pid = await AddParticipantAsync(tc2026MeetId, alpha26M3Slug, _alphaTeamId);
        await RecordFullTotalAsync(tc2026MeetId, alpha26M3Pid, 170.0m, 130.0m, 200.0m); // Total=500 → Place 3 → 8pts

        string alpha26M4Slug = await CreateAthleteAsync("Alpha26M4", "m");
        int alpha26M4Pid = await AddParticipantAsync(tc2026MeetId, alpha26M4Slug, _alphaTeamId);
        await RecordFullTotalAsync(tc2026MeetId, alpha26M4Pid, 150.0m, 120.0m, 180.0m); // Total=450 → Place 4 → 7pts

        string alpha26M5Slug = await CreateAthleteAsync("Alpha26M5", "m");
        int alpha26M5Pid = await AddParticipantAsync(tc2026MeetId, alpha26M5Slug, _alphaTeamId);
        await RecordFullTotalAsync(tc2026MeetId, alpha26M5Pid, 135.0m, 110.0m, 155.0m); // Total=400 → Place 5 → 6pts

        string alpha26M6Slug = await CreateAthleteAsync("Alpha26M6", "m");
        int alpha26M6Pid = await AddParticipantAsync(tc2026MeetId, alpha26M6Slug, _alphaTeamId);
        await RecordFullTotalAsync(tc2026MeetId, alpha26M6Pid, 120.0m, 100.0m, 130.0m); // Total=350 → Place 6 → 5pts
    }

    public async ValueTask DisposeAsync()
    {
        // Delete meets first (cascades participations)
        foreach (string slug in _meetSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{slug}", CancellationToken.None);
        }

        // Delete athletes
        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        // Delete teams
        foreach (string slug in _teamSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/teams/{slug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WithCorrectStandings()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsGenderSplit_ForMeetIn2025()
    {
        // Arrange

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.IsGenderSplit.ShouldBeTrue();
        response.Women.ShouldNotBeEmpty();
        response.Men.ShouldNotBeEmpty();
        response.Combined.ShouldBeEmpty();
    }

    [Fact]
    public async Task RanksMenTeamsByTotalPoints()
    {
        // Arrange

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert
        response!.Men.Count.ShouldBe(2);
        response.Men[0].TeamName.ShouldBe(_alphaTeamName);
        response.Men[0].TotalPoints.ShouldBe(12);
        response.Men[0].Rank.ShouldBe(1);
        response.Men[1].TeamName.ShouldBe(_betaTeamName);
        response.Men[1].TotalPoints.ShouldBe(8);
        response.Men[1].Rank.ShouldBe(2);
    }

    [Fact]
    public async Task RanksWomenTeamsByTotalPoints()
    {
        // Arrange

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert
        response!.Women.Count.ShouldBe(2);
        response.Women[0].TeamName.ShouldBe(_alphaTeamName);
        response.Women[0].TotalPoints.ShouldBe(12);
        response.Women[0].Rank.ShouldBe(1);
        response.Women[1].TeamName.ShouldBe(_betaTeamName);
        response.Women[1].TotalPoints.ShouldBe(9);
        response.Women[1].Rank.ShouldBe(2);
    }

    [Fact]
    public async Task ReturnsEmptyLists_WhenMeetHasNoTeamPoints()
    {
        // Arrange

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_noParticipationsMeetSlug}/team-points", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Women.ShouldBeEmpty();
        response.Men.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnsNotFound_ForUnknownSlug()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/nonexistent-meet/team-points", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AppliesBestNLimit_PerMeet()
    {
        // Arrange — 6 male athletes with places 1–6 → points 12,9,8,7,6,5; best 5 = 12+9+8+7+6 = 42

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2026MeetSlug}/team-points", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Men.ShouldNotBeEmpty();
        TeamCompetitionStanding alpha = response.Men.First(s => s.TeamName == _alphaTeamName);
        alpha.TotalPoints.ShouldBe(ExpectedBestN2026TotalPoints);
    }

    [Fact]
    public async Task ExcludesDisqualifiedParticipations()
    {
        // Arrange — DQ'd Alpha male has Place=0, TeamPoints=0
        // Alpha men total should be 12 (from Place 1 only, not including DQ'd participant)

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert
        response!.Men[0].TeamName.ShouldBe(_alphaTeamName);
        response.Men[0].TotalPoints.ShouldBe(12);
    }

    [Fact]
    public async Task ExcludesParticipationsWithNoTeam()
    {
        // Arrange — a participation without TeamId exists

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert — only 2 teams in men
        response!.Men.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExcludesParticipationsWithZeroTeamPoints()
    {
        // Arrange — Gamma has only a participation with no recorded attempts (TeamPoints=null)
        // If the TeamPoints filter were removed, Gamma would appear as a third team

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert — only Alpha and Beta appear; Gamma (with no team points) is excluded
        response!.Men.Count.ShouldBe(2);
        response.Men.ShouldNotContain(s => s.TeamSlug == _gammaTeamSlug);
    }

    private async Task RecordFullTotalAsync(
        int meetId,
        int participationId,
        decimal squat,
        decimal bench,
        decimal deadlift)
    {
        await RecordAttemptAsync(meetId, participationId, Discipline.Squat, 1, squat);
        await RecordAttemptAsync(meetId, participationId, Discipline.Bench, 1, bench);
        await RecordAttemptAsync(meetId, participationId, Discipline.Deadlift, 1, deadlift);
    }

    private async Task RecordAttemptAsync(int meetId, int participationId, Discipline discipline, int round, decimal weight, bool good = true)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(good)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private async Task<int> CreateTeamAsync(string title, string titleShort)
    {
        Contracts.Teams.CreateTeamCommand command = new CreateTeamCommandBuilder()
            .WithTitle(title)
            .WithTitleShort(titleShort)
            .WithTitleFull(title)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/teams", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        _teamSlugs.Add(Slug.Create(title));

        string location = response.Headers.Location!.ToString().TrimStart('/');
        return int.Parse(location, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<int> CreateMeetAndGetIdAsync(DateOnly startDate)
    {
        string slug = await CreateMeetSlugAsync(startDate);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", CancellationToken.None);

        return meetDetails!.MeetId;
    }

    private async Task<string> CreateMeetSlugAsync(DateOnly startDate)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsInTeamCompetition(true)
            .WithShowTeamPoints(true)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);
        return slug;
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Tp";

        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender(gender)
            .WithDateOfBirth(new DateOnly(1990, 1, 1))
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{firstName} {lastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<int> AddParticipantAsync(
        int meetId,
        string athleteSlug,
        int? teamId,
        decimal bodyWeight = 82.0m)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .WithTeamId(teamId)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }
}