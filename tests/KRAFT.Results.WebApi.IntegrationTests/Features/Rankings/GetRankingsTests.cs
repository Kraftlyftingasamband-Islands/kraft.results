using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.Rankings;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    private const decimal P1Total = P1Squat + P1Bench + P1Deadlift;
    private const decimal P2Squat = 180.0m;
    private const decimal P2Bench = 120.0m;
    private const decimal P2Deadlift = 230.0m;
    private const decimal P3Squat = 180.0m;
    private const decimal P3Bench = 120.0m;
    private const decimal P3Deadlift = 230.0m;

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<int> _meetIds = [];

    public async ValueTask InitializeAsync()
    {
        string athleteASlug = await CreateAthleteAsync("RnkA", "m");
        string athleteBSlug = await CreateAthleteAsync("RnkB", "m");

        int meet1Id = await CreateMeetAndGetIdAsync(new DateOnly(MeetYear, 6, 1));
        int meet2Id = await CreateMeetAndGetIdAsync(new DateOnly(MeetYear, 9, 1));

        // P1: athlete A in meet1, place 1, best result
        int p1Id = await AddParticipantAsync(meet1Id, athleteASlug);
        await RecordAttemptAsync(meet1Id, p1Id, Discipline.Squat, 1, P1Squat);
        await RecordAttemptAsync(meet1Id, p1Id, Discipline.Bench, 1, P1Bench);
        await RecordAttemptAsync(meet1Id, p1Id, Discipline.Deadlift, 1, P1Deadlift);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Place = 1 WHERE ParticipationId = {p1Id}");

        // P2: athlete A in meet2, place 1, second-best result
        int p2Id = await AddParticipantAsync(meet2Id, athleteASlug);
        await RecordAttemptAsync(meet2Id, p2Id, Discipline.Squat, 1, P2Squat);
        await RecordAttemptAsync(meet2Id, p2Id, Discipline.Bench, 1, P2Bench);
        await RecordAttemptAsync(meet2Id, p2Id, Discipline.Deadlift, 1, P2Deadlift);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Place = 1 WHERE ParticipationId = {p2Id}");

        // P3: athlete B in meet1, disqualified
        int p3Id = await AddParticipantAsync(meet1Id, athleteBSlug);
        await RecordAttemptAsync(meet1Id, p3Id, Discipline.Squat, 1, P3Squat);
        await RecordAttemptAsync(meet1Id, p3Id, Discipline.Bench, 1, P3Bench);
        await RecordAttemptAsync(meet1Id, p3Id, Discipline.Deadlift, 1, P3Deadlift);
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Place = 3, Disqualified = 1 WHERE ParticipationId = {p3Id}");
    }

    public async ValueTask DisposeAsync()
    {
        // Delete in FK order: Records → Attempts → Participations → Meets → Athletes
        if (_meetIds.Count > 0)
        {
            await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
            ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

            string meetIdList = string.Join(", ", _meetIds);
            string cleanupSql =
                $"""
                DELETE FROM Records WHERE AttemptId IN (
                    SELECT AttemptId FROM Attempts WHERE ParticipationId IN (
                        SELECT ParticipationId FROM Participations WHERE MeetId IN ({meetIdList})
                    )
                );
                DELETE FROM Attempts WHERE ParticipationId IN (
                    SELECT ParticipationId FROM Participations WHERE MeetId IN ({meetIdList})
                );
                DELETE FROM Participations WHERE MeetId IN ({meetIdList});
                """;

            await dbContext.Database.ExecuteSqlRawAsync(cleanupSql);
        }

        foreach (string slug in _meetSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{slug}", CancellationToken.None);
        }

        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
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

        // Assert — Wilks is not computed by the RecordAttempt flow, so it remains 0
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Wilks.ShouldBe(0m);
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

    private async Task<int> CreateMeetAndGetIdAsync(DateOnly startDate)
    {
        string slug = await CreateMeetSlugAsync(startDate);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", CancellationToken.None);

        int meetId = meetDetails!.MeetId;
        _meetIds.Add(meetId);
        return meetId;
    }

    private async Task<string> CreateMeetSlugAsync(DateOnly startDate)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsRaw(true)
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

    private async Task RecordAttemptAsync(int meetId, int participationId, Discipline discipline, int round, decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }
}