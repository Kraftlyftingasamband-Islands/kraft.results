using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(GetRecordsByEraTestsCollection))]
public sealed class GetRecordsByEraTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string RecordsPath = "/records";

    // Squat weights per era and weight category (referenced by assertions)
    private const decimal CurrentSquatWeight83 = 210.0m;
    private const decimal CurrentSquatWeight93 = 225.0m;
    private const decimal HistoricalSquatWeight83 = 185.0m;
    private const decimal HistoricalSquatWeight105 = 260.0m;

    // Supporting lift weights (not directly asserted)
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;
    private const decimal HistoricalBenchWeight = 100.0m;
    private const decimal HistoricalDeadliftWeight = 200.0m;

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        string athleteSlug = await CreateAthleteAsync("EraA", "m", new DateOnly(1985, 7, 2));

        // Current era meets (2019+)
        int currentMeet83Id = await CreateMeetAndGetIdAsync(new DateOnly(2025, 3, 15), isRaw: false);
        int currentMeet93Id = await CreateMeetAndGetIdAsync(new DateOnly(2025, 3, 15), isRaw: false);

        // Historical era meets (2011-2018)
        int historicalMeet83Id = await CreateMeetAndGetIdAsync(new DateOnly(2017, 6, 15), isRaw: false);
        int historicalMeet105Id = await CreateMeetAndGetIdAsync(new DateOnly(2018, 3, 10), isRaw: false);

        // Current era, 83kg (body weight 80.5 -> 83kg category)
        int p1Id = await AddParticipantAsync(currentMeet83Id, athleteSlug, 80.5m);
        _participations.Add((currentMeet83Id, p1Id));
        await RecordAttemptAsync(currentMeet83Id, p1Id, Discipline.Squat, 1, CurrentSquatWeight83);
        await RecordAttemptAsync(currentMeet83Id, p1Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(currentMeet83Id, p1Id, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Current era, 93kg (body weight 90.0 -> 93kg category)
        int p2Id = await AddParticipantAsync(currentMeet93Id, athleteSlug, 90.0m);
        _participations.Add((currentMeet93Id, p2Id));
        await RecordAttemptAsync(currentMeet93Id, p2Id, Discipline.Squat, 1, CurrentSquatWeight93);
        await RecordAttemptAsync(currentMeet93Id, p2Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(currentMeet93Id, p2Id, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Historical era, 83kg (body weight 80.5 -> 83kg category)
        int p3Id = await AddParticipantAsync(historicalMeet83Id, athleteSlug, 80.5m);
        _participations.Add((historicalMeet83Id, p3Id));
        await RecordAttemptAsync(historicalMeet83Id, p3Id, Discipline.Squat, 1, HistoricalSquatWeight83);
        await RecordAttemptAsync(historicalMeet83Id, p3Id, Discipline.Bench, 1, HistoricalBenchWeight);
        await RecordAttemptAsync(historicalMeet83Id, p3Id, Discipline.Deadlift, 1, HistoricalDeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Historical era, 105kg (body weight 100.0 -> 105kg category)
        int p4Id = await AddParticipantAsync(historicalMeet105Id, athleteSlug, 100.0m);
        _participations.Add((historicalMeet105Id, p4Id));
        await RecordAttemptAsync(historicalMeet105Id, p4Id, Discipline.Squat, 1, HistoricalSquatWeight105);
        await RecordAttemptAsync(historicalMeet105Id, p4Id, Discipline.Bench, 1, HistoricalBenchWeight);
        await RecordAttemptAsync(historicalMeet105Id, p4Id, Discipline.Deadlift, 1, HistoricalDeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach ((int meetId, int participationId) in _participations)
        {
            await _authorizedHttpClient.DeleteAsync(
                $"/meets/{meetId}/participants/{participationId}", CancellationToken.None);
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
    public async Task ReturnsCurrentEraRecords_WhenNoEraSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        groups.ShouldContain(g => g.Category == "Hnébeygja");
    }

    [Fact]
    public async Task ReturnsHistoricalEraRecords_WhenEraSlugSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hnébeygja");
        squatGroup.Records.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsHistoricalEraRecords_WithCorrectWeightCategories()
    {
        // Arrange — historical era has 83kg and 105kg weight categories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldContain(r => r.WeightCategory == "83");
        allRecords.ShouldContain(r => r.WeightCategory == "105");
    }

    [Fact]
    public async Task DoesNotReturnCurrentEraRecords_ForHistoricalEra()
    {
        // Arrange — current era has 93kg records, historical does not

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "93");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenEraSlugIsUnknown()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped&era=nonexistent-era",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsCurrentEraRecords_WhenExplicitCurrentEraSlugSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped&era=current-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        groups.ShouldContain(g => g.Category == "Hnébeygja");
    }

    [Fact]
    public async Task HistoricalEraExcludes105kg_FromCurrentEra()
    {
        // Arrange — 105kg is NOT in current era's EraWeightCategories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "105");
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender, DateOnly dateOfBirth)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Rc";

        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender(gender)
            .WithDateOfBirth(dateOfBirth)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{firstName} {lastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<int> CreateMeetAndGetIdAsync(DateOnly startDate, bool isRaw)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsRaw(isRaw)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", CancellationToken.None);

        return meetDetails!.MeetId;
    }

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug, decimal bodyWeight)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }

    private async Task RecordAttemptAsync(
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight)
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