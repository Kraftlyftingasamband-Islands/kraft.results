using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetTeamPointsTests(CollectionFixture fixture)
{
    private const string BasePath = "/meets";

    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WithCorrectStandings()
    {
        // Arrange
        string slug = Constants.TeamCompetition.TcMeet12025Slug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/team-points", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsGenderSplit_ForMeetIn2025()
    {
        // Arrange
        string slug = Constants.TeamCompetition.TcMeet12025Slug;

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{slug}/team-points", CancellationToken.None);

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
        // Arrange — tc-meet-1-2025 (MeetId 2):
        // Alpha male: 12 points, Beta male: 8 points

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{Constants.TeamCompetition.TcMeet12025Slug}/team-points", CancellationToken.None);

        // Assert
        response!.Men.Count.ShouldBe(2);
        response.Men[0].TeamName.ShouldBe("Alpha Team");
        response.Men[0].TotalPoints.ShouldBe(12);
        response.Men[0].Rank.ShouldBe(1);
        response.Men[1].TeamName.ShouldBe("Beta Team");
        response.Men[1].TotalPoints.ShouldBe(8);
        response.Men[1].Rank.ShouldBe(2);
    }

    [Fact]
    public async Task RanksWomenTeamsByTotalPoints()
    {
        // Arrange — tc-meet-1-2025 (MeetId 2):
        // Alpha female: 12 points, Beta female: 9 points

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{Constants.TeamCompetition.TcMeet12025Slug}/team-points", CancellationToken.None);

        // Assert
        response!.Women.Count.ShouldBe(2);
        response.Women[0].TeamName.ShouldBe("Alpha Team");
        response.Women[0].TotalPoints.ShouldBe(12);
        response.Women[0].Rank.ShouldBe(1);
        response.Women[1].TeamName.ShouldBe("Beta Team");
        response.Women[1].TotalPoints.ShouldBe(9);
        response.Women[1].Rank.ShouldBe(2);
    }

    [Fact]
    public async Task ReturnsEmptyLists_WhenMeetHasNoTeamPoints()
    {
        // Arrange — test-meet-2025 exists but has no team points data

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{Constants.TestMeetSlug}/team-points", CancellationToken.None);

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
        // Arrange — tc-meet-1-2026 (MeetId 4):
        // Alpha Team: 6 male athletes all scoring 12 → best 5 → 5*12 = 60

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{Constants.TeamCompetition.TcMeet12026Slug}/team-points", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Men.ShouldNotBeEmpty();
        TeamCompetitionStanding alpha = response.Men.First(s => s.TeamName == "Alpha Team");
        alpha.TotalPoints.ShouldBe(60);
    }

    [Fact]
    public async Task ExcludesDisqualifiedParticipations()
    {
        // Arrange — tc-meet-1-2025 has a DQ'd participation with TeamPoints=7 for Alpha
        // Alpha men should have 12 (not 12+7=19)

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{Constants.TeamCompetition.TcMeet12025Slug}/team-points", CancellationToken.None);

        // Assert
        response!.Men[0].TeamName.ShouldBe("Alpha Team");
        response.Men[0].TotalPoints.ShouldBe(12);
    }

    [Fact]
    public async Task ExcludesParticipationsWithNoTeam()
    {
        // Arrange — tc-meet-1-2025 has a participation without TeamId

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{Constants.TeamCompetition.TcMeet12025Slug}/team-points", CancellationToken.None);

        // Assert — only 2 teams in men
        response!.Men.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExcludesParticipationsWithZeroTeamPoints()
    {
        // Arrange — tc-meet-1-2025 has a participation with TeamPoints=0 for Alpha

        // Act
        MeetTeamPointsResponse? response = await _httpClient.GetFromJsonAsync<MeetTeamPointsResponse>(
            $"{BasePath}/{Constants.TeamCompetition.TcMeet12025Slug}/team-points", CancellationToken.None);

        // Assert — Alpha men should have 12 (not including the 0-point entry)
        response!.Men[0].TotalPoints.ShouldBe(12);
    }
}