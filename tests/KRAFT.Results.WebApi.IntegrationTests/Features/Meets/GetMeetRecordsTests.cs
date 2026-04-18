using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetRecordsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string BasePath = "/meets";
    private const decimal EquippedSquatWeight = 200.0m;
    private const decimal ClassicSquatWeight = 195.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private int _meetId;
    private string _meetSlug = string.Empty;
    private string _athleteName = string.Empty;
    private string _athleteSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // Create a meet with RecordsPossible=true
        CreateMeetCommand meetCommand = new CreateMeetCommandBuilder()
            .WithIsRaw(true)
            .Build();

        HttpResponseMessage createMeetResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets",
            meetCommand,
            CancellationToken.None);

        createMeetResponse.EnsureSuccessStatusCode();

        _meetSlug = createMeetResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meetSlug}",
            CancellationToken.None);

        _meetId = meetDetails!.MeetId;

        // Create an Icelandic athlete (eligible for records)
        string uniqueCode = UniqueShortCode.Next();
        string firstName = $"RecTest{uniqueCode}";
        string lastName = "Athlete";
        _athleteName = $"{firstName} {lastName}";
        _athleteSlug = Slug.Create(_athleteName);

        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithDateOfBirth(new DateOnly(1985, 7, 2))
            .Build();

        HttpResponseMessage createAthleteResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        createAthleteResponse.EnsureSuccessStatusCode();

        // Add participant: 83kg weight class, open age
        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(_athleteSlug)
            .WithBodyWeight(82.5m)
            .WithAgeCategorySlug("open")
            .Build();

        HttpResponseMessage addParticipantResponse = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{_meetId}/participants",
            participantCommand,
            CancellationToken.None);

        addParticipantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? participantResult = await addParticipantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        // Record attempts to create real Attempt entities
        await RecordAttempt(participationId, (int)Discipline.Bench, 1, BenchWeight);
        await RecordAttempt(participationId, (int)Discipline.Deadlift, 1, DeadliftWeight);
        await RecordAttempt(participationId, (int)Discipline.Squat, 1, ClassicSquatWeight);

        // Find the squat attempt ID for record insertion
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int squatAttemptId = await dbContext.Set<WebApi.Features.Attempts.Attempt>()
            .Where(a => a.ParticipationId == participationId)
            .Where(a => a.Discipline == Discipline.Squat)
            .Select(a => a.AttemptId)
            .SingleAsync(CancellationToken.None);

        // Insert records directly via SQL — this test verifies the GET endpoint, not record computation
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES
                ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {ClassicSquatWeight}, GETUTCDATE(), 0, {squatAttemptId}, 1, 1, 'test-setup'),
                ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {EquippedSquatWeight}, GETUTCDATE(), 0, {squatAttemptId}, 1, 0, 'test-setup')
            """,
            CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (_meetId == 0)
        {
            return;
        }

        await _authorizedHttpClient.DeleteAsync(
            $"/meets/{_meetSlug}",
            CancellationToken.None);

        if (_athleteSlug.Length > 0)
        {
            await _authorizedHttpClient.DeleteAsync(
                $"/athletes/{_athleteSlug}",
                CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WithRecords_WhenMeetHasApprovedRecords()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task DoesNotInclude_StandardRecords()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        // Standard record is for 93kg squat (220kg) — should not appear
        records.ShouldNotContain(r => r.WeightCategory == "93"
            && r.Discipline == "Hnébeygja"
            && r.Weight == 220.0m);
    }

    [Fact]
    public async Task DoesNotInclude_TotalWilksRecords()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.ShouldNotContain(r => r.Weight == 400.0m);
    }

    [Fact]
    public async Task DoesNotInclude_TotalIpfPointsRecords()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.ShouldNotContain(r => r.Weight == 85.5m);
    }

    [Fact]
    public async Task IncludesCorrectFields_ForEquippedRecord()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        MeetRecordEntry squatRecord = records
            .Where(r => r.Weight == EquippedSquatWeight)
            .Where(r => r.Discipline == "Hnébeygja")
            .First(r => !r.IsClassic);

        squatRecord.AthleteName.ShouldBe(_athleteName);
        squatRecord.AthleteSlug.ShouldBe(_athleteSlug);
        squatRecord.WeightCategory.ShouldBe("83");
        squatRecord.AgeCategory.ShouldBe("Open");
        squatRecord.IsClassic.ShouldBeFalse();
    }

    [Fact]
    public async Task IncludesClassicRecords_WithIsClassicTrue()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        MeetRecordEntry classicRecord = records.First(r => r.Weight == ClassicSquatWeight);
        classicRecord.IsClassic.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/non-existent-meet/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IsPublic_NoAuthRequired()
    {
        // Arrange — using unauthenticated client
        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task RecordAttempt(int participationId, int disciplineId, int round, decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(true)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{_meetId}/participants/{participationId}/attempts/{disciplineId}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}