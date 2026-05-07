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

namespace KRAFT.Results.WebApi.IntegrationTests.Features.TeamCompetition;

[Collection(nameof(TeamCompetitionCollection))]
public sealed class GetTeamCompetitionTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string BasePath = "/team-competition";
    private const int Year2025 = 2025;
    private const int Year2026 = 2026;

    // 2025 men — meet1: alphaMale=Place1→12pts, noTeam=Place2→9pts(excluded), betaMale=Place3→8pts
    //            meet2: betaMale=Place1→12pts, alphaMale=Place2→9pts
    // Totals: Alpha=12+9=21, Beta=8+12=20
    private const int AlphaMenMeet1Points = 12;
    private const int AlphaMenMeet2Points = 9;
    private const int AlphaMenTotalPoints = AlphaMenMeet1Points + AlphaMenMeet2Points; // 21
    private const int BetaMenMeet1Points = 8;
    private const int BetaMenMeet2Points = 12;
    private const int BetaMenTotalPoints = BetaMenMeet1Points + BetaMenMeet2Points; // 20

    // 2025 women — meet1: alphaFemale=Place1→12pts, betaFemale=Place2→9pts
    private const int AlphaWomenPoints = 12;
    private const int BetaWomenPoints = 9;

    // BestN 2026:
    // Meet1: 6 athletes, totals descend → places 1–6 → points 12,9,8,7,6,5 → best 5 = 12+9+8+7+6 = 42
    // Meet2: 3 athletes, totals descend → places 1–3 → points 12,9,8 → sum = 29
    // Grand total: 42 + 29 = 71
    private const int BestNMeet1Total = 42; // 12+9+8+7+6
    private const int BestNMeet2Total = 29; // 12+9+8
    private const int BestNExpectedTotal = BestNMeet1Total + BestNMeet2Total; // 71

    // Meet1 2025 — men totals (same weight category, 82.0m body weight)
    private const decimal AlphaMaleMeet1Squat = 100.0m;
    private const decimal AlphaMaleMeet1Bench = 100.0m;
    private const decimal AlphaMaleMeet1Deadlift = 100.0m; // Total=300 → Place 1

    private const decimal NoTeamMaleMeet1Squat = 90.0m;
    private const decimal NoTeamMaleMeet1Bench = 80.0m;
    private const decimal NoTeamMaleMeet1Deadlift = 80.0m; // Total=250 → Place 2 (no team, excluded from standings)

    private const decimal BetaMaleMeet1Squat = 70.0m;
    private const decimal BetaMaleMeet1Bench = 60.0m;
    private const decimal BetaMaleMeet1Deadlift = 70.0m; // Total=200 → Place 3

    // Meet2 2025 — men totals (same weight category, 82.0m body weight)
    private const decimal BetaMaleMeet2Squat = 100.0m;
    private const decimal BetaMaleMeet2Bench = 100.0m;
    private const decimal BetaMaleMeet2Deadlift = 100.0m; // Total=300 → Place 1

    private const decimal AlphaMaleMeet2Squat = 90.0m;
    private const decimal AlphaMaleMeet2Bench = 80.0m;
    private const decimal AlphaMaleMeet2Deadlift = 80.0m; // Total=250 → Place 2

    // Meet1 2025 — women totals (57.0m body weight)
    private const decimal AlphaFemaleMeet1Squat = 90.0m;
    private const decimal AlphaFemaleMeet1Bench = 70.0m;
    private const decimal AlphaFemaleMeet1Deadlift = 100.0m; // Total=260 → Place 1

    private const decimal BetaFemaleMeet1Squat = 80.0m;
    private const decimal BetaFemaleMeet1Bench = 60.0m;
    private const decimal BetaFemaleMeet1Deadlift = 80.0m; // Total=220 → Place 2

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly string _alphaShortCode = UniqueShortCode.Next();
    private readonly string _betaShortCode = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<string> _teamSlugs = [];

    private string _alphaTeamName = string.Empty;
    private string _alphaTeamSlug = string.Empty;
    private string _betaTeamName = string.Empty;
    private int _alphaTeamId;
    private int _betaTeamId;

    public async ValueTask InitializeAsync()
    {
        // Create teams
        _alphaTeamName = $"Alpha{_suffix}";
        _alphaTeamSlug = Slug.Create(_alphaTeamName);
        _alphaTeamId = await CreateTeamAsync(_alphaTeamName, _alphaShortCode);

        _betaTeamName = $"Beta{_suffix}";
        _betaTeamId = await CreateTeamAsync(_betaTeamName, _betaShortCode);

        // Create 2025 meets (two meets in same year for cross-meet totals)
        int meet1In2025Id = await CreateMeetAndGetIdAsync(new DateOnly(Year2025, 6, 1));
        int meet2In2025Id = await CreateMeetAndGetIdAsync(new DateOnly(Year2025, 9, 1));

        // Create athletes for 2025
        string alphaMaleSlug = await CreateAthleteAsync("AlphaM", "m");
        string betaMaleSlug = await CreateAthleteAsync("BetaM", "m");
        string alphaFemaleSlug = await CreateAthleteAsync("AlphaF", "f");
        string betaFemaleSlug = await CreateAthleteAsync("BetaF", "f");
        string dqAlphaMaleSlug = await CreateAthleteAsync("DqAlphaM", "m");
        string noTeamSlug = await CreateAthleteAsync("NoTeam", "m");
        string zeroPtsSlug = await CreateAthleteAsync("ZeroPts", "m");

        // Add participants for 2025 meet1 — men (82.0m body weight, same weight category)
        int alphaMalePid1 = await AddParticipantAsync(meet1In2025Id, alphaMaleSlug, _alphaTeamId);
        int betaMalePid1 = await AddParticipantAsync(meet1In2025Id, betaMaleSlug, _betaTeamId);
        int dqPid = await AddParticipantAsync(meet1In2025Id, dqAlphaMaleSlug, _alphaTeamId);
        int noTeamPid = await AddParticipantAsync(meet1In2025Id, noTeamSlug, teamId: null);
        await AddParticipantAsync(meet1In2025Id, zeroPtsSlug, _alphaTeamId);

        // DQ the dqAlphaMale via 3 failed squat attempts → Disqualified=true, Total=0, Place=0, TeamPoints=0
        await RecordAttemptAsync(meet1In2025Id, dqPid, Discipline.Squat, 1, 100.0m, good: false);
        await RecordAttemptAsync(meet1In2025Id, dqPid, Discipline.Squat, 2, 100.0m, good: false);
        await RecordAttemptAsync(meet1In2025Id, dqPid, Discipline.Squat, 3, 100.0m, good: false);

        // Meet1 men totals determine places:
        // alphaMale: Total=300 → Place 1 → TeamPoints=12
        // noTeamMale: Total=250 → Place 2 → TeamPoints=9 (no team, excluded from standings)
        // betaMale: Total=200 → Place 3 → TeamPoints=8
        // zeroPts: no attempts → Total=0, not disqualified → Place=0, TeamPoints=null (excluded)
        await RecordFullTotalAsync(meet1In2025Id, alphaMalePid1, AlphaMaleMeet1Squat, AlphaMaleMeet1Bench, AlphaMaleMeet1Deadlift);
        await RecordFullTotalAsync(meet1In2025Id, noTeamPid, NoTeamMaleMeet1Squat, NoTeamMaleMeet1Bench, NoTeamMaleMeet1Deadlift);
        await RecordFullTotalAsync(meet1In2025Id, betaMalePid1, BetaMaleMeet1Squat, BetaMaleMeet1Bench, BetaMaleMeet1Deadlift);

        // Add participants for 2025 meet1 — women (57.0m body weight, separate weight category)
        int alphaFemalePid = await AddParticipantAsync(meet1In2025Id, alphaFemaleSlug, _alphaTeamId, 57.0m);
        int betaFemalePid = await AddParticipantAsync(meet1In2025Id, betaFemaleSlug, _betaTeamId, 57.0m);

        // Meet1 women totals:
        // alphaFemale: Total=260 → Place 1 → TeamPoints=12
        // betaFemale: Total=220 → Place 2 → TeamPoints=9
        await RecordFullTotalAsync(meet1In2025Id, alphaFemalePid, AlphaFemaleMeet1Squat, AlphaFemaleMeet1Bench, AlphaFemaleMeet1Deadlift);
        await RecordFullTotalAsync(meet1In2025Id, betaFemalePid, BetaFemaleMeet1Squat, BetaFemaleMeet1Bench, BetaFemaleMeet1Deadlift);

        // Add participants for 2025 meet2 — men (82.0m body weight)
        int alphaMalePid2 = await AddParticipantAsync(meet2In2025Id, alphaMaleSlug, _alphaTeamId);
        int betaMalePid2 = await AddParticipantAsync(meet2In2025Id, betaMaleSlug, _betaTeamId);

        // Meet2 men totals:
        // betaMale: Total=300 → Place 1 → TeamPoints=12
        // alphaMale: Total=250 → Place 2 → TeamPoints=9
        await RecordFullTotalAsync(meet2In2025Id, betaMalePid2, BetaMaleMeet2Squat, BetaMaleMeet2Bench, BetaMaleMeet2Deadlift);
        await RecordFullTotalAsync(meet2In2025Id, alphaMalePid2, AlphaMaleMeet2Squat, AlphaMaleMeet2Bench, AlphaMaleMeet2Deadlift);

        // Create 2026 meets for BestN test
        int meet1In2026Id = await CreateMeetAndGetIdAsync(new DateOnly(Year2026, 6, 1));
        int meet2In2026Id = await CreateMeetAndGetIdAsync(new DateOnly(Year2026, 9, 1));

        // Meet1 2026: 6 male athletes with descending totals → places 1–6 → points 12,9,8,7,6,5
        // Best 5 of those 6 = 12+9+8+7+6 = 42
        string alpha26M1Slug = await CreateAthleteAsync("Alpha26M1", "m");
        int alpha26M1Pid = await AddParticipantAsync(meet1In2026Id, alpha26M1Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet1In2026Id, alpha26M1Pid, 200.0m, 150.0m, 250.0m); // Total=600 → Place 1 → 12pts

        string alpha26M2Slug = await CreateAthleteAsync("Alpha26M2", "m");
        int alpha26M2Pid = await AddParticipantAsync(meet1In2026Id, alpha26M2Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet1In2026Id, alpha26M2Pid, 190.0m, 140.0m, 220.0m); // Total=550 → Place 2 → 9pts

        string alpha26M3Slug = await CreateAthleteAsync("Alpha26M3", "m");
        int alpha26M3Pid = await AddParticipantAsync(meet1In2026Id, alpha26M3Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet1In2026Id, alpha26M3Pid, 170.0m, 130.0m, 200.0m); // Total=500 → Place 3 → 8pts

        string alpha26M4Slug = await CreateAthleteAsync("Alpha26M4", "m");
        int alpha26M4Pid = await AddParticipantAsync(meet1In2026Id, alpha26M4Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet1In2026Id, alpha26M4Pid, 150.0m, 120.0m, 180.0m); // Total=450 → Place 4 → 7pts

        string alpha26M5Slug = await CreateAthleteAsync("Alpha26M5", "m");
        int alpha26M5Pid = await AddParticipantAsync(meet1In2026Id, alpha26M5Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet1In2026Id, alpha26M5Pid, 135.0m, 110.0m, 155.0m); // Total=400 → Place 5 → 6pts

        string alpha26M6Slug = await CreateAthleteAsync("Alpha26M6", "m");
        int alpha26M6Pid = await AddParticipantAsync(meet1In2026Id, alpha26M6Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet1In2026Id, alpha26M6Pid, 120.0m, 100.0m, 130.0m); // Total=350 → Place 6 → 5pts (capped by BestN)

        // Meet2 2026: 3 male athletes with descending totals → places 1–3 → points 12,9,8 → sum=29
        string alpha26N0Slug = await CreateAthleteAsync("Alpha26N0", "m");
        int alpha26N0Pid = await AddParticipantAsync(meet2In2026Id, alpha26N0Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet2In2026Id, alpha26N0Pid, 100.0m, 100.0m, 100.0m); // Total=300 → Place 1 → 12pts

        string alpha26N1Slug = await CreateAthleteAsync("Alpha26N1", "m");
        int alpha26N1Pid = await AddParticipantAsync(meet2In2026Id, alpha26N1Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet2In2026Id, alpha26N1Pid, 90.0m, 80.0m, 80.0m); // Total=250 → Place 2 → 9pts

        string alpha26N2Slug = await CreateAthleteAsync("Alpha26N2", "m");
        int alpha26N2Pid = await AddParticipantAsync(meet2In2026Id, alpha26N2Slug, _alphaTeamId);
        await RecordFullTotalAsync(meet2In2026Id, alpha26N2Pid, 70.0m, 60.0m, 70.0m); // Total=200 → Place 3 → 8pts
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
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Year.ShouldBe(Year2025);
    }

    [Fact]
    public async Task ReturnsGenderSplit_ForYear2025()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert
        response!.IsGenderSplit.ShouldBeTrue();
        response.Women.ShouldNotBeEmpty();
        response.Men.ShouldNotBeEmpty();
        response.Combined.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnsCombined_ForYearBefore2015()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/2014", CancellationToken.None);

        // Assert
        response!.IsGenderSplit.ShouldBeFalse();
        response.Women.ShouldBeEmpty();
        response.Men.ShouldBeEmpty();
    }

    [Fact]
    public async Task RanksTeamsByTotalPointsDescending_Men()
    {
        // Arrange — Alpha: 12+9=21, Beta: 8+12=20

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert
        response!.Men.Count.ShouldBeGreaterThanOrEqualTo(2);
        TeamCompetitionStanding alphaStanding = response.Men.First(s => s.TeamName == _alphaTeamName);
        TeamCompetitionStanding betaStanding = response.Men.First(s => s.TeamName == _betaTeamName);
        alphaStanding.TotalPoints.ShouldBe(AlphaMenTotalPoints);
        betaStanding.TotalPoints.ShouldBe(BetaMenTotalPoints);
        alphaStanding.Rank.ShouldBeLessThan(betaStanding.Rank);
    }

    [Fact]
    public async Task RanksTeamsByTotalPointsDescending_Women()
    {
        // Arrange — Alpha: 12, Beta: 9

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert
        response!.Women.Count.ShouldBeGreaterThanOrEqualTo(2);
        TeamCompetitionStanding alphaStanding = response.Women.First(s => s.TeamName == _alphaTeamName);
        TeamCompetitionStanding betaStanding = response.Women.First(s => s.TeamName == _betaTeamName);
        alphaStanding.TotalPoints.ShouldBe(AlphaWomenPoints);
        betaStanding.TotalPoints.ShouldBe(BetaWomenPoints);
        alphaStanding.Rank.ShouldBeLessThan(betaStanding.Rank);
    }

    [Fact]
    public async Task ExcludesDisqualifiedParticipations()
    {
        // Arrange — DQ'd participation has Place=0, TeamPoints=0 for Alpha
        // Alpha men: 12+9=21 (not 12+9+0=21+DQ contribution)

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert
        TeamCompetitionStanding alphaStanding = response!.Men.First(s => s.TeamName == _alphaTeamName);
        alphaStanding.TotalPoints.ShouldBe(AlphaMenTotalPoints);
    }

    [Fact]
    public async Task ExcludesParticipationsWithNoTeam()
    {
        // Arrange — participation without TeamId should be excluded

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert — no-team participation must not create a phantom entry with null/empty slug
        response!.Men.ShouldAllBe(s => !string.IsNullOrEmpty(s.TeamSlug));
    }

    [Fact]
    public async Task ExcludesParticipationsWithZeroTeamPoints()
    {
        // Arrange — zeroPts participant has no attempts → TeamPoints=null → excluded
        // If it were included, the zero-point entry would still appear in the team's breakdown

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert — only Alpha and Beta appear; no phantom teams from zero-point entries
        TeamCompetitionStanding alphaStanding = response!.Men.First(s => s.TeamName == _alphaTeamName);
        alphaStanding.TotalPoints.ShouldBe(AlphaMenTotalPoints);
        response.Men.ShouldAllBe(s => s.TotalPoints > 0);
    }

    [Fact]
    public async Task ReturnsEmptyLists_WhenNoDataForYear()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/1999", CancellationToken.None);

        // Assert
        response!.Combined.ShouldBeEmpty();
    }

    [Fact]
    public async Task IncludesTeamSlug()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert
        TeamCompetitionStanding alphaStanding = response!.Men.First(s => s.TeamName == _alphaTeamName);
        alphaStanding.TeamSlug.ShouldBe(_alphaTeamSlug);
    }

    [Fact]
    public async Task AppliesBestNLimit_PerMeet()
    {
        // Arrange — Alpha Team 2026 men:
        // Meet1: 6 athletes, places 1–6 → points 12,9,8,7,6,5 → best 5 → 12+9+8+7+6 = 42
        // Meet2: 3 athletes, places 1–3 → points 12,9,8 → sum = 29
        // Grand total: 42 + 29 = 71

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2026}", CancellationToken.None);

        // Assert
        response!.Men.ShouldNotBeEmpty();
        TeamCompetitionStanding alpha = response.Men.First(s => s.TeamName == _alphaTeamName);
        alpha.TotalPoints.ShouldBe(BestNExpectedTotal);
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
        string lastName = "Tc";

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