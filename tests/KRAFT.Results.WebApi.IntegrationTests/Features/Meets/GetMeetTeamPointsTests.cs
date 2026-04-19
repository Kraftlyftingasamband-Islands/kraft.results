using System.Net;
using System.Net.Http.Json;

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

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly string _alphaShortCode = UniqueShortCode.Next();
    private readonly string _betaShortCode = UniqueShortCode.Next();
    private readonly string _gammaShortCode = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];

    private string _alphaTeamName = string.Empty;
    private string _alphaTeamSlug = string.Empty;
    private string _betaTeamName = string.Empty;
    private string _betaTeamSlug = string.Empty;
    private string _gammaTeamSlug = string.Empty;
    private int _alphaTeamId;
    private int _betaTeamId;
    private int _gammaTeamId;

    private int _tc2025MeetId;
    private string _tc2025MeetSlug = string.Empty;
    private int _tc2026MeetId;
    private string _tc2026MeetSlug = string.Empty;
    private string _nonTcMeetSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // Create teams
        _alphaTeamName = $"Alpha{_suffix}";
        _alphaTeamSlug = Slug.Create(_alphaTeamName);
        _alphaTeamId = await CreateTeamAsync(_alphaTeamName, _alphaShortCode);

        _betaTeamName = $"Beta{_suffix}";
        _betaTeamSlug = Slug.Create(_betaTeamName);
        _betaTeamId = await CreateTeamAsync(_betaTeamName, _betaShortCode);

        string gammaTeamName = $"Gamma{_suffix}";
        _gammaTeamSlug = Slug.Create(gammaTeamName);
        _gammaTeamId = await CreateTeamAsync(gammaTeamName, _gammaShortCode);

        // Create 3 meets
        _tc2025MeetSlug = await CreateMeetAsync(new DateOnly(2025, 6, 1));
        MeetDetails? tc2025Details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_tc2025MeetSlug}", CancellationToken.None);
        _tc2025MeetId = tc2025Details!.MeetId;

        _tc2026MeetSlug = await CreateMeetAsync(new DateOnly(2026, 6, 1));
        MeetDetails? tc2026Details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_tc2026MeetSlug}", CancellationToken.None);
        _tc2026MeetId = tc2026Details!.MeetId;

        _nonTcMeetSlug = await CreateMeetAsync(new DateOnly(2025, 3, 1));

        // Create athletes for 2025 meet (7 unique athletes)
        string alphaMaleSlug = await CreateAthleteAsync("AlphaM", "m");
        string betaMaleSlug = await CreateAthleteAsync("BetaM", "m");
        string alphaFemaleSlug = await CreateAthleteAsync("AlphaF", "f");
        string betaFemaleSlug = await CreateAthleteAsync("BetaF", "f");
        string dqAlphaMaleSlug = await CreateAthleteAsync("DqAlphaM", "m");
        string noTeamSlug = await CreateAthleteAsync("NoTeam", "m");
        string zeroPtsSlug = await CreateAthleteAsync("ZeroPts", "m");

        // Add participations for 2025 meet
        int alphaMalePid = await AddParticipantAsync(_tc2025MeetId, alphaMaleSlug, _alphaTeamId);
        int betaMalePid = await AddParticipantAsync(_tc2025MeetId, betaMaleSlug, _betaTeamId);
        int alphaFemalePid = await AddParticipantAsync(_tc2025MeetId, alphaFemaleSlug, _alphaTeamId, 57.0m);
        int betaFemalePid = await AddParticipantAsync(_tc2025MeetId, betaFemaleSlug, _betaTeamId, 57.0m);
        int dqAlphaMalePid = await AddParticipantAsync(_tc2025MeetId, dqAlphaMaleSlug, _alphaTeamId);
        int noTeamPid = await AddParticipantAsync(_tc2025MeetId, noTeamSlug, teamId: null);
        int zeroPtsPid = await AddParticipantAsync(_tc2025MeetId, zeroPtsSlug, _gammaTeamId);

        // Set TeamPoints, Place, Disqualified via SQL for 2025 meet
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = 12, Place = 1 WHERE ParticipationId = {alphaMalePid}");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = 8, Place = 3 WHERE ParticipationId = {betaMalePid}");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = 12, Place = 1 WHERE ParticipationId = {alphaFemalePid}");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = 9, Place = 2 WHERE ParticipationId = {betaFemalePid}");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Disqualified = 1, TeamPoints = 7 WHERE ParticipationId = {dqAlphaMalePid}");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = 5 WHERE ParticipationId = {noTeamPid}");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET TeamPoints = 0 WHERE ParticipationId = {zeroPtsPid}");

        // Create 6 male athletes for 2026 meet and add participations
        for (int i = 1; i <= 6; i++)
        {
            string slug = await CreateAthleteAsync($"Alpha26M{i}", "m");
            int pid = await AddParticipantAsync(_tc2026MeetId, slug, _alphaTeamId);
            await fixture.ExecuteSqlAsync(
                $"UPDATE Participations SET TeamPoints = 12, Place = {i} WHERE ParticipationId = {pid}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Delete meets first (cascades participations)
        if (!string.IsNullOrEmpty(_tc2025MeetSlug))
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{_tc2025MeetSlug}", CancellationToken.None);
        }

        if (!string.IsNullOrEmpty(_tc2026MeetSlug))
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{_tc2026MeetSlug}", CancellationToken.None);
        }

        if (!string.IsNullOrEmpty(_nonTcMeetSlug))
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{_nonTcMeetSlug}", CancellationToken.None);
        }

        // Delete athletes
        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        // Delete teams
        if (!string.IsNullOrEmpty(_alphaTeamSlug))
        {
            await _authorizedHttpClient.DeleteAsync($"/teams/{_alphaTeamSlug}", CancellationToken.None);
        }

        if (!string.IsNullOrEmpty(_betaTeamSlug))
        {
            await _authorizedHttpClient.DeleteAsync($"/teams/{_betaTeamSlug}", CancellationToken.None);
        }

        if (!string.IsNullOrEmpty(_gammaTeamSlug))
        {
            await _authorizedHttpClient.DeleteAsync($"/teams/{_gammaTeamSlug}", CancellationToken.None);
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
            $"{BasePath}/{_nonTcMeetSlug}/team-points", CancellationToken.None);

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
        // Arrange — 6 male athletes all scoring 12 -> best 5 -> 5*12 = 60

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2026MeetSlug}/team-points", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Men.ShouldNotBeEmpty();
        TeamCompetitionStanding alpha = response.Men.First(s => s.TeamName == _alphaTeamName);
        alpha.TotalPoints.ShouldBe(60);
    }

    [Fact]
    public async Task ExcludesDisqualifiedParticipations()
    {
        // Arrange — DQ'd participation with TeamPoints=7 for Alpha
        // Alpha men should have 12 (not 12+7=19)

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
        // Arrange — Gamma has only a participation with TeamPoints=0
        // If the TeamPoints > 0 filter were removed, Gamma would appear as a third team

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{_tc2025MeetSlug}/team-points", CancellationToken.None);

        // Assert — only Alpha and Beta appear; Gamma (with zero points) is excluded
        response!.Men.Count.ShouldBe(2);
        response.Men.ShouldNotContain(s => s.TeamSlug == _gammaTeamSlug);
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

        string location = response.Headers.Location!.ToString().TrimStart('/');
        return int.Parse(location, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<string> CreateMeetAsync(DateOnly startDate)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsInTeamCompetition(true)
            .WithShowTeamPoints(true)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return response.Headers.Location!.ToString().TrimStart('/');
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

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug, int? teamId, decimal bodyWeight = 82.0m)
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