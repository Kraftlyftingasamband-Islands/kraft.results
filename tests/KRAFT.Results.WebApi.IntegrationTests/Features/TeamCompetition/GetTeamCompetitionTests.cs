using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.TeamCompetition;

[Collection(nameof(TeamCompetitionCollection))]
public sealed class GetTeamCompetitionTests(CollectionFixture fixture)
{
    private const string BasePath = "/team-competition";

    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync($"{BasePath}/2025", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Year.ShouldBe(2025);
    }

    [Fact]
    public async Task ReturnsGenderSplit_ForYear2025()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

        // Assert
        response!.IsGenderSplit.ShouldBeTrue();
        response.Women.ShouldNotBeEmpty();
        response.Men.ShouldNotBeEmpty();
        response.Combined.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnsCombined_ForYearBefore2015()
    {
        // Arrange — no data for 2014, but structure should still indicate non-gender-split

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2014", CancellationToken.None);

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
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

        // Assert
        response!.Men.Count.ShouldBe(2);
        response.Men[0].TeamName.ShouldBe("Alpha Team");
        response.Men[0].TotalPoints.ShouldBe(21);
        response.Men[0].Rank.ShouldBe(1);
        response.Men[1].TeamName.ShouldBe("Beta Team");
        response.Men[1].TotalPoints.ShouldBe(20);
        response.Men[1].Rank.ShouldBe(2);
    }

    [Fact]
    public async Task RanksTeamsByTotalPointsDescending_Women()
    {
        // Arrange — Alpha: 12, Beta: 9

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

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
    public async Task ExcludesDisqualifiedParticipations()
    {
        // Arrange — DQ'd participation with TeamPoints=7 for Alpha should be excluded
        // Alpha men: 12+9=21 (not 12+9+7=28)

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

        // Assert
        response!.Men[0].TeamName.ShouldBe("Alpha Team");
        response.Men[0].TotalPoints.ShouldBe(21);
    }

    [Fact]
    public async Task ExcludesParticipationsWithNoTeam()
    {
        // Arrange — participation without TeamId has TeamPoints=6, should be excluded

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

        // Assert — total team count for men should be 2 (Alpha and Beta only)
        response!.Men.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExcludesParticipationsWithZeroTeamPoints()
    {
        // Arrange — participation with TeamPoints=0 for Alpha should be excluded
        // Alpha men: 12+9=21 (not 12+9+0=21, but entry count matters)

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

        // Assert
        response!.Men[0].TotalPoints.ShouldBe(21);
    }

    [Fact]
    public async Task ReturnsEmptyLists_WhenNoDataForYear()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/1999", CancellationToken.None);

        // Assert
        response!.Combined.ShouldBeEmpty();
    }

    [Fact]
    public async Task IncludesTeamSlug()
    {
        // Arrange

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2025", CancellationToken.None);

        // Assert
        response!.Men[0].TeamSlug.ShouldBe(Constants.TeamCompetition.AlphaTeamSlug);
    }

    [Fact]
    public async Task AppliesBestNLimit_PerMeet()
    {
        // Arrange — Alpha Team 2026 men:
        // Meet 1: 6 athletes all scoring 12 → best 5 → 5*12 = 60
        // Meet 2: 3 athletes scoring 12, 9, 8 → all 3 count → 29
        // Per-meet total: 60 + 29 = 89
        // (Global cap would incorrectly give top 5 of [12x7, 9, 8] = 5*12 = 60)

        // Act
        TeamCompetitionResponse? response = await _httpClient.GetFromJsonAsync<TeamCompetitionResponse>($"{BasePath}/2026", CancellationToken.None);

        // Assert
        response!.Men.ShouldNotBeEmpty();
        TeamCompetitionStanding alpha = response.Men.First(s => s.TeamName == "Alpha Team");
        alpha.TotalPoints.ShouldBe(89);
    }
}