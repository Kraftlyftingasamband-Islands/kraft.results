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
    private const int AlphaMenMeet1Points = 12;
    private const int AlphaMenMeet2Points = 9;
    private const int AlphaMenTotalPoints = AlphaMenMeet1Points + AlphaMenMeet2Points; // 21
    private const int BetaMenMeet1Points = 8;
    private const int BetaMenMeet2Points = 12;
    private const int BetaMenTotalPoints = BetaMenMeet1Points + BetaMenMeet2Points; // 20
    private const int AlphaWomenPoints = 12;
    private const int BetaWomenPoints = 9;
    private const int DqPoints = 7;
    private const int NoTeamPoints = 5;
    private const int ZeroTeamPoints = 0;
    private const int BestNPointsPerAthlete = 12;
    private const int BestNMeet2Athlete1Points = 12;
    private const int BestNMeet2Athlete2Points = 9;
    private const int BestNMeet2Athlete3Points = 8;
    private const int BestNLimit = 5;
    private const int BestNMeet1Total = BestNLimit * BestNPointsPerAthlete; // 60
    private const int BestNMeet2Total = BestNMeet2Athlete1Points + BestNMeet2Athlete2Points + BestNMeet2Athlete3Points; // 29
    private const int BestNExpectedTotal = BestNMeet1Total + BestNMeet2Total; // 89

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

        // Alpha male: meet1=12, meet2=9 → total=21
        int alphaMalePid1 = await AddParticipantAsync(meet1In2025Id, alphaMaleSlug, _alphaTeamId);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {AlphaMenMeet1Points}, Place = 1 WHERE ParticipationId = {alphaMalePid1}");

        int alphaMalePid2 = await AddParticipantAsync(meet2In2025Id, alphaMaleSlug, _alphaTeamId);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {AlphaMenMeet2Points}, Place = 2 WHERE ParticipationId = {alphaMalePid2}");

        // Beta male: meet1=8, meet2=12 → total=20
        int betaMalePid1 = await AddParticipantAsync(meet1In2025Id, betaMaleSlug, _betaTeamId);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {BetaMenMeet1Points}, Place = 3 WHERE ParticipationId = {betaMalePid1}");

        int betaMalePid2 = await AddParticipantAsync(meet2In2025Id, betaMaleSlug, _betaTeamId);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {BetaMenMeet2Points}, Place = 1 WHERE ParticipationId = {betaMalePid2}");

        // Alpha female: meet1=12
        int alphaFemalePid = await AddParticipantAsync(meet1In2025Id, alphaFemaleSlug, _alphaTeamId, 57.0m);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {AlphaWomenPoints}, Place = 1 WHERE ParticipationId = {alphaFemalePid}");

        // Beta female: meet1=9
        int betaFemalePid = await AddParticipantAsync(meet1In2025Id, betaFemaleSlug, _betaTeamId, 57.0m);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {BetaWomenPoints}, Place = 2 WHERE ParticipationId = {betaFemalePid}");

        // DQ'd participation: Alpha male, meet1, TeamPoints=7 (should be excluded)
        // 3 failed squat attempts trigger automatic DQ via RecalculateTotals
        int dqPid = await AddParticipantAsync(meet1In2025Id, dqAlphaMaleSlug, _alphaTeamId);
        await RecordAttemptAsync(meet1In2025Id, dqPid, Discipline.Squat, 1, 100.0m, good: false);
        await RecordAttemptAsync(meet1In2025Id, dqPid, Discipline.Squat, 2, 100.0m, good: false);
        await RecordAttemptAsync(meet1In2025Id, dqPid, Discipline.Squat, 3, 100.0m, good: false);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {DqPoints} WHERE ParticipationId = {dqPid}");

        // No team participation: meet1, TeamPoints=5 (should be excluded)
        int noTeamPid = await AddParticipantAsync(meet1In2025Id, noTeamSlug, teamId: null);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {NoTeamPoints} WHERE ParticipationId = {noTeamPid}");

        // Zero team points participation: meet1, TeamPoints=0 (should be excluded)
        int zeroPtsPid = await AddParticipantAsync(meet1In2025Id, zeroPtsSlug, _alphaTeamId);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = {ZeroTeamPoints} WHERE ParticipationId = {zeroPtsPid}");

        // Create 2026 meets for BestN test
        int meet1In2026Id = await CreateMeetAndGetIdAsync(new DateOnly(Year2026, 6, 1));
        int meet2In2026Id = await CreateMeetAndGetIdAsync(new DateOnly(Year2026, 9, 1));

        // Meet1 2026: 6 male athletes all scoring 12 → best 5 → 60
        for (int i = 1; i <= 6; i++)
        {
            string slug = await CreateAthleteAsync($"Alpha26M{i}", "m");
            int pid = await AddParticipantAsync(meet1In2026Id, slug, _alphaTeamId);
            await fixture.ExecuteSqlAsync(
                $"UPDATE Participations SET TeamPoints = {BestNPointsPerAthlete}, Place = {i} WHERE ParticipationId = {pid}");
        }

        // Meet2 2026: 3 male athletes scoring 12, 9, 8 → 29
        int[] meet2Points = [BestNMeet2Athlete1Points, BestNMeet2Athlete2Points, BestNMeet2Athlete3Points];
        for (int i = 0; i < meet2Points.Length; i++)
        {
            string slug = await CreateAthleteAsync($"Alpha26N{i}", "m");
            int pid = await AddParticipantAsync(meet2In2026Id, slug, _alphaTeamId);
            await fixture.ExecuteSqlAsync(
                $"UPDATE Participations SET TeamPoints = {meet2Points[i]}, Place = {i + 1} WHERE ParticipationId = {pid}");
        }
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
        // Arrange — DQ'd participation with TeamPoints=7 for Alpha should be excluded
        // Alpha men: 12+9=21 (not 12+9+7=28)

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
        // Arrange — participation without TeamId has TeamPoints=5, should be excluded

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2025}", CancellationToken.None);

        // Assert — no-team participation must not create a phantom entry with null/empty slug
        response!.Men.ShouldAllBe(s => !string.IsNullOrEmpty(s.TeamSlug));
    }

    [Fact]
    public async Task ExcludesParticipationsWithZeroTeamPoints()
    {
        // Arrange — participation with TeamPoints=0 for Alpha should be excluded
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
        // Meet 1: 6 athletes all scoring 12 -> best 5 -> 5*12 = 60
        // Meet 2: 3 athletes scoring 12, 9, 8 -> all 3 count -> 29
        // Per-meet total: 60 + 29 = 89

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>(
            $"{BasePath}/{Year2026}", CancellationToken.None);

        // Assert
        response!.Men.ShouldNotBeEmpty();
        TeamCompetitionStanding alpha = response.Men.First(s => s.TeamName == _alphaTeamName);
        alpha.TotalPoints.ShouldBe(BestNExpectedTotal);
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