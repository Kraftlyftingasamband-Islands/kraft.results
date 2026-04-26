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

[Collection(nameof(GetRecordHistoryTestsCollection))]
public sealed class GetRecordHistoryTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string RecordsPath = "/records";
    private const decimal OldestSquatWeight = 180.0m;
    private const decimal MiddleSquatWeight = 195.0m;
    private const decimal CurrentSquatWeight = 210.0m;
    private const decimal BenchWeight = 100.0m;
    private const decimal DeadliftWeight = 200.0m;

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;
    private int _currentRecordId;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        string athleteSlug = await CreateAthleteAsync("HistA", "m", new DateOnly(1985, 7, 2));

        // Three classic meets with increasing dates — each produces progressively higher squat records
        int meet1Id = await CreateMeetAndGetIdAsync(new DateOnly(2023, 6, 10), isRaw: true);
        int meet2Id = await CreateMeetAndGetIdAsync(new DateOnly(2024, 3, 15), isRaw: true);
        int meet3Id = await CreateMeetAndGetIdAsync(new DateOnly(2025, 3, 15), isRaw: true);

        // Meet 1: oldest squat record (180.0)
        int p1Id = await AddParticipantAsync(meet1Id, athleteSlug, 90.0m);
        _participations.Add((meet1Id, p1Id));
        await RecordAttemptAsync(meet1Id, p1Id, Discipline.Squat, 1, OldestSquatWeight);
        await RecordAttemptAsync(meet1Id, p1Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(meet1Id, p1Id, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Meet 2: middle squat record (195.0)
        int p2Id = await AddParticipantAsync(meet2Id, athleteSlug, 90.0m);
        _participations.Add((meet2Id, p2Id));
        await RecordAttemptAsync(meet2Id, p2Id, Discipline.Squat, 1, MiddleSquatWeight);
        await RecordAttemptAsync(meet2Id, p2Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(meet2Id, p2Id, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Meet 3: current squat record (210.0)
        int p3Id = await AddParticipantAsync(meet3Id, athleteSlug, 90.0m);
        _participations.Add((meet3Id, p3Id));
        await RecordAttemptAsync(meet3Id, p3Id, Discipline.Squat, 1, CurrentSquatWeight);
        await RecordAttemptAsync(meet3Id, p3Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(meet3Id, p3Id, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Discover the current squat record ID from the records endpoint
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=classic",
            CancellationToken.None);

        RecordEntry squatRecord = groups!
            .First(g => g.Category == "Hnébeygja")
            .Records
            .First(r => r.WeightCategory == "93");

        _currentRecordId = squatRecord.Id;
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
    public async Task ReturnsOk_WithValidRecordId()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{RecordsPath}/{_currentRecordId}/history",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsNotFound_WithInvalidRecordId()
    {
        // Arrange
        int invalidId = 999999;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{RecordsPath}/{invalidId}/history",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsEntriesOrderedByDate()
    {
        // Arrange

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{_currentRecordId}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Entries.ShouldNotBeEmpty();

        List<DateOnly> dates = history.Entries
            .Select(e => e.Date)
            .ToList();

        dates.ShouldBe(dates.OrderBy(d => d).ToList());
    }

    [Fact]
    public async Task CurrentRecordIsMarked()
    {
        // Arrange

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{_currentRecordId}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Entries.Count(e => e.IsCurrent).ShouldBe(1);
    }

    [Fact]
    public async Task ResponseIncludesMetadata()
    {
        // Arrange

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{_currentRecordId}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Category.ShouldNotBeNullOrWhiteSpace();
        history.WeightCategory.ShouldNotBeNullOrWhiteSpace();
        history.AgeCategory.ShouldNotBeNullOrWhiteSpace();
        history.Gender.ShouldNotBeNullOrWhiteSpace();
        history.EquipmentType.ShouldNotBeNullOrWhiteSpace();
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