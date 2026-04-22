using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.Rankings;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Rankings;

[Collection(nameof(RankingsCollection))]
public sealed class GetRankingsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/rankings";
    private const int MeetYear = 2020;
    private const decimal P1Squat = 200.0m;
    private const decimal P1Bench = 130.0m;
    private const decimal P1Deadlift = 250.0m;
    private const decimal P1Total = 580.0m;
    private const decimal P1Wilks = 400.0m;
    private const decimal P2Total = 550.0m;
    private const decimal P2Wilks = 370.0m;

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];

    private string _meet1Slug = string.Empty;
    private string _meet2Slug = string.Empty;
    private int _meet1Id;
    private int _meet2Id;

    public async ValueTask InitializeAsync()
    {
        string athleteASlug = await CreateAthleteAsync("RnkA", "m");
        string athleteBSlug = await CreateAthleteAsync("RnkB", "m");

        _meet1Slug = await CreateMeetAsync(new DateOnly(MeetYear, 6, 1));
        MeetDetails? meet1Details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meet1Slug}", CancellationToken.None);
        _meet1Id = meet1Details!.MeetId;

        _meet2Slug = await CreateMeetAsync(new DateOnly(MeetYear, 9, 1));
        MeetDetails? meet2Details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meet2Slug}", CancellationToken.None);
        _meet2Id = meet2Details!.MeetId;

        // P1: athlete A in meet1, place 1, best result
        int p1Id = await AddParticipantAsync(_meet1Id, athleteASlug);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Squat = {P1Squat}, Benchpress = {P1Bench}, Deadlift = {P1Deadlift}, Total = {P1Total}, Wilks = {P1Wilks}, Place = 1 WHERE ParticipationId = {p1Id}");

        // P2: athlete A in meet2, place 1, second-best result
        int p2Id = await AddParticipantAsync(_meet2Id, athleteASlug);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Squat = 180.0, Benchpress = 120.0, Deadlift = 230.0, Total = {P2Total}, Wilks = {P2Wilks}, Place = 1 WHERE ParticipationId = {p2Id}");

        // P3: athlete B in meet1, disqualified
        int p3Id = await AddParticipantAsync(_meet1Id, athleteBSlug);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Squat = 180.0, Benchpress = 120.0, Deadlift = 230.0, Total = 530.0, Wilks = 360.0, Place = 3, Disqualified = 1 WHERE ParticipationId = {p3Id}");
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up via SQL for reliability — cascade-deleting meets removes participations
        if (_meet1Id != 0)
        {
            await fixture.ExecuteSqlAsync(
                $"DELETE FROM Participations WHERE MeetId = {_meet1Id}");
            await fixture.ExecuteSqlAsync(
                $"DELETE FROM Meets WHERE MeetId = {_meet1Id}");
        }

        if (_meet2Id != 0)
        {
            await fixture.ExecuteSqlAsync(
                $"DELETE FROM Participations WHERE MeetId = {_meet2Id}");
            await fixture.ExecuteSqlAsync(
                $"DELETE FROM Meets WHERE MeetId = {_meet2Id}");
        }

        foreach (string slug in _athleteSlugs)
        {
            await fixture.ExecuteSqlAsync(
                $"DELETE FROM Athletes WHERE Slug = {slug}");
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
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task DefaultsToTotalDiscipline()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(P1Total);
    }

    [Fact]
    public async Task FiltersByYear()
    {
        // Arrange
        int differentYear = MeetYear + 1;

        // Act
        PagedResponse<RankingEntry>? matchingYear = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);
        PagedResponse<RankingEntry>? differentYearResponse = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={differentYear}", CancellationToken.None);

        // Assert
        matchingYear!.Items.ShouldNotBeEmpty();
        differentYearResponse!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenYearHasNoData()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year=1900", CancellationToken.None);

        // Assert
        response!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task FiltersByGender()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&gender=m", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenGenderHasNoData()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&gender=f", CancellationToken.None);

        // Assert
        response!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task FiltersByEquipmentType()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&equipmentType=classic", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenEquipmentTypeHasNoData()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&equipmentType=equipped", CancellationToken.None);

        // Assert
        response!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task FiltersByDiscipline_Squat()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&discipline=squat", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(P1Squat);
    }

    [Fact]
    public async Task FiltersByDiscipline_Bench()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&discipline=bench", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(P1Bench);
    }

    [Fact]
    public async Task FiltersByDiscipline_Deadlift()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&discipline=deadlift", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(P1Deadlift);
    }

    [Fact]
    public async Task ExcludesDisqualified()
    {
        // Arrange — athlete B is DQ'd, only athlete A should appear

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert
        response!.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ShowsBestResultPerAthlete()
    {
        // Arrange — athlete A has two non-DQ participations in separate meets

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert — grouping keeps only the best (highest calculated IPF points, P1 with 580 total)
        response!.Items.Count.ShouldBe(1);
        response.Items[0].IpfPoints.ShouldNotBeNull();
        response.Items[0].IpfPoints!.Value.ShouldBeGreaterThan(0m);
        response.Items[0].Result.ShouldBe(P1Total);
    }

    [Fact]
    public async Task OrdersByIpfPointsDescending()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].IpfPoints.ShouldNotBeNull();
        response.Items[0].IpfPoints!.Value.ShouldBeGreaterThan(0m);
        response.Items[0].Rank.ShouldBe(1);
    }

    [Fact]
    public async Task IncludesWilks()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Wilks.ShouldBe(P1Wilks);
    }

    [Fact]
    public async Task Paginates()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}&page=1&pageSize=1", CancellationToken.None);

        // Assert
        response!.Items.Count.ShouldBe(1);
        response.Page.ShouldBe(1);
        response.PageSize.ShouldBe(1);
    }

    [Fact]
    public async Task OrdersDescendingByResult()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(
            $"{Path}?year={MeetYear}", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Rank.ShouldBe(1);
    }

    private async Task<string> CreateMeetAsync(DateOnly startDate)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsRaw(true)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return response.Headers.Location!.ToString().TrimStart('/');
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Rk";

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

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(80.5m)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }
}