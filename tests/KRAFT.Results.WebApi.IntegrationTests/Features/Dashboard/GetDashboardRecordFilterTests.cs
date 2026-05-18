using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Dashboard;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Dashboard;

/// <summary>
/// Tests that the dashboard filters to only current (IsCurrent=true) records.
///
/// Setup: one female athlete sets records in a meet, then we directly mark her squat record
/// as IsCurrent=false (simulating it having been superseded).  With the IsCurrent filter
/// absent, the superseded squat record would appear among the latest-records widget entries
/// because there are so few female records in the test DB (fewer than 3 current ones).
/// </summary>
[Collection(nameof(GetDashboardRecordsTestsCollection))]
public sealed class GetDashboardRecordFilterTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string DashboardPath = "/dashboard";
    private const string MeetsPath = "/meets";

    private const decimal SquatWeight = 62.5m;

    // 63f weight category (body weight 57.01–63.00 kg)
    private const decimal BodyWeight63f = 60.0m;

    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private HttpClient _httpClient = null!;
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;
    private int _meetId;

    public async ValueTask InitializeAsync()
    {
        _httpClient = fixture.Factory!.CreateClient();
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        string firstName = $"DF{_suffix}";
        string lastName = "Rc";

        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender("f")
            .WithDateOfBirth(new DateOnly(1990, 1, 1))
            .Build();

        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", athleteCommand, CancellationToken.None);
        athleteResponse.EnsureSuccessStatusCode();
        _athleteSlugs.Add(Slug.Create($"{firstName} {lastName}"));

        _meetId = await CreateMeetAndGetIdAsync(new DateOnly(2022, 6, 1), isRaw: false);

        int participationId = await AddParticipantAsync(_meetId, Slug.Create($"{firstName} {lastName}"), BodyWeight63f);
        _participations.Add((_meetId, participationId));

        // Record all 3 lifts to produce a valid total and create records (squat, bench, deadlift, total, singles)
        await RecordAttemptAsync(_meetId, participationId, Discipline.Squat, 1, SquatWeight);
        await RecordAttemptAsync(_meetId, participationId, Discipline.Bench, 1, 40.0m);
        await RecordAttemptAsync(_meetId, participationId, Discipline.Deadlift, 1, 80.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Simulate the squat record being superseded: mark it IsCurrent=false directly.
        // Filter by meetId to avoid affecting records from other test runs.
        // This is the scenario the dashboard IsCurrent filter must handle.
        await fixture.ExecuteSqlAsync(
            $"""
            UPDATE r
            SET r.IsCurrent = 0
            FROM Records r
            INNER JOIN Attempts a ON a.AttemptId = r.AttemptId
            INNER JOIN Participations p ON p.ParticipationId = a.ParticipationId
            WHERE p.MeetId = {_meetId}
              AND r.RecordCategoryId = 1
              AND r.IsCurrent = 1
            """);
    }

    public async ValueTask DisposeAsync()
    {
        foreach ((int meetId, int participationId) in _participations)
        {
            await _authorizedHttpClient.DeleteAsync(
                $"{MeetsPath}/{meetId}/participants/{participationId}", CancellationToken.None);
        }

        foreach (string slug in _meetSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"{MeetsPath}/{slug}", CancellationToken.None);
        }

        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task LatestRecordsWomen_ExcludesSupersededRecord()
    {
        // Arrange
        // The athlete's squat record at SquatWeight was marked IsCurrent=false in InitializeAsync,
        // simulating a superseded record. Without the IsCurrent filter in the query, this record
        // would appear among the top-3 female records (the test DB has very few female records).

        // Act
        DashboardSummary? result = await _httpClient.GetFromJsonAsync<DashboardSummary>(
            DashboardPath, CancellationToken.None);

        // Assert
        DashboardSummary dashboard = result.ShouldNotBeNull();
        dashboard.LatestRecordsWomen.ShouldNotContain(r => r.Weight == SquatWeight);
    }

    private async Task<int> CreateMeetAndGetIdAsync(DateOnly startDate, bool isRaw)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsRaw(isRaw)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            MeetsPath, command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"{MeetsPath}/{slug}", CancellationToken.None);

        return meetDetails!.MeetId;
    }

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug, decimal bodyWeight)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"{MeetsPath}/{meetId}/participants", command, CancellationToken.None);
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
            $"{MeetsPath}/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }
}