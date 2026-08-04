using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

[Collection(nameof(GetAthleteRecordsTestsCollection))]
public sealed class GetAthleteRecordsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const decimal SquatWeight = 200.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;

    // Open athlete — born 1990, age 35 at test meet → resolves to "open" only
    private static readonly DateOnly OpenAthleteDob = new(1990, 6, 15);

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;

    private string _openAthleteSlug = string.Empty;
    private int _meetId;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        _openAthleteSlug = await CreateAthleteAsync("RecOp", "m", OpenAthleteDob);
        _meetId = await CreateMeetAndGetIdAsync(new DateOnly(2025, 3, 15), isRaw: false);

        // Open athlete at 83 kg bodyweight → 83 kg weight category
        int participationId = await AddParticipantAsync(_meetId, _openAthleteSlug, 80.5m);
        _participations.Add((_meetId, participationId));
        await RecordAttemptAsync(_meetId, participationId, Discipline.Squat, 1, SquatWeight);
        await RecordAttemptAsync(_meetId, participationId, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(_meetId, participationId, Discipline.Deadlift, 1, DeadliftWeight);
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
    public async Task WhenAthleteHasRecords_ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"/athletes/{_openAthleteSlug}/records",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenAthleteHasOpenRecord_AgeCategoryIsTranslatedToIcelandic()
    {
        // Arrange

        // Act
        List<AthleteRecord>? records = await _httpClient.GetFromJsonAsync<List<AthleteRecord>>(
            $"/athletes/{_openAthleteSlug}/records",
            TestContext.Current.CancellationToken);

        // Assert
        records.ShouldNotBeNull();
        records.ShouldNotBeEmpty();
        records.ShouldAllBe(r => r.AgeCategory == "Opinn flokkur");
    }

    [Fact]
    public async Task WhenAthleteDoesNotExist_ReturnsNotFound()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"/athletes/does-not-exist-{_suffix}/records",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender, DateOnly dateOfBirth)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Rec";

        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender(gender)
            .WithDateOfBirth(dateOfBirth)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", command, TestContext.Current.CancellationToken);
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
            "/meets", command, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", TestContext.Current.CancellationToken);

        return meetDetails!.MeetId;
    }

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug, decimal bodyWeight)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(TestContext.Current.CancellationToken);

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
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
