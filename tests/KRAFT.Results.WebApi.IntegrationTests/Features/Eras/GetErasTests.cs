using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Eras;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Eras;

[Collection(nameof(GetErasTestsCollection))]
public sealed class GetErasTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string ErasPath = "/eras";
    private const decimal SquatWeight = 200.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;

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

        string athleteSlug = await CreateAthleteAsync("Era", "m", new DateOnly(1990, 1, 1));

        // Historical era meet (2011-01-01 to 2018-12-31)
        int historicalMeetId = await CreateMeetAndGetIdAsync(new DateOnly(2017, 6, 15), isRaw: true);

        int p1Id = await AddParticipantAsync(historicalMeetId, athleteSlug, 80.5m);
        _participations.Add((historicalMeetId, p1Id));
        await RecordAttemptAsync(historicalMeetId, p1Id, Discipline.Squat, 1, SquatWeight);
        await RecordAttemptAsync(historicalMeetId, p1Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(historicalMeetId, p1Id, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Current era meet (2019-01-01 to 2099-12-31)
        int currentMeetId = await CreateMeetAndGetIdAsync(new DateOnly(2025, 3, 15), isRaw: true);

        int p2Id = await AddParticipantAsync(currentMeetId, athleteSlug, 80.5m);
        _participations.Add((currentMeetId, p2Id));
        await RecordAttemptAsync(currentMeetId, p2Id, Discipline.Squat, 1, SquatWeight);
        await RecordAttemptAsync(currentMeetId, p2Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(currentMeetId, p2Id, Discipline.Deadlift, 1, DeadliftWeight);
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
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            ErasPath,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsAllEras()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            ErasPath,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        eras.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ReturnsErasOrderedByStartDate()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            ErasPath,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        eras[0].Slug.ShouldBe("historical-era");
        eras[1].Slug.ShouldBe("current-era");
    }

    [Fact]
    public async Task ReturnsEraSummaryWithAllFields()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            ErasPath,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        EraSummary historicalEra = eras.First(e => e.Slug == "historical-era");
        historicalEra.Title.ShouldBe("Historical Era");
        historicalEra.StartDate.ShouldBe(new DateOnly(2011, 1, 1));
        historicalEra.EndDate.ShouldBe(new DateOnly(2018, 12, 31));
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender, DateOnly dateOfBirth)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Er";

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