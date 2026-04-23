using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
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
    private const decimal StandardSquatWeight = 220.0m;
    private const decimal TotalWilksWeight = 400.0m;
    private const decimal TotalIpfPointsWeight = 85.5m;

    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;
    private int _rawMeetId;
    private string _rawMeetSlug = string.Empty;
    private int _equippedMeetId;
    private string _equippedMeetSlug = string.Empty;
    private string _athleteName = string.Empty;
    private string _athleteSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        // Create a raw meet (IsRaw=true) — records from this meet have IsClassic=true
        CreateMeetCommand rawMeetCommand = new CreateMeetCommandBuilder()
            .WithIsRaw(true)
            .Build();

        HttpResponseMessage createRawMeetResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets",
            rawMeetCommand,
            CancellationToken.None);

        createRawMeetResponse.EnsureSuccessStatusCode();

        _rawMeetSlug = createRawMeetResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? rawMeetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_rawMeetSlug}",
            CancellationToken.None);

        _rawMeetId = rawMeetDetails!.MeetId;

        // Create an equipped meet (IsRaw=false) — records from this meet have IsClassic=false
        CreateMeetCommand equippedMeetCommand = new CreateMeetCommandBuilder()
            .WithIsRaw(false)
            .Build();

        HttpResponseMessage createEquippedMeetResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets",
            equippedMeetCommand,
            CancellationToken.None);

        createEquippedMeetResponse.EnsureSuccessStatusCode();

        _equippedMeetSlug = createEquippedMeetResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? equippedMeetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_equippedMeetSlug}",
            CancellationToken.None);

        _equippedMeetId = equippedMeetDetails!.MeetId;

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

        // Add participant to raw meet: 83kg weight class, open age
        AddParticipantCommand rawParticipantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(_athleteSlug)
            .WithBodyWeight(82.5m)
            .WithAgeCategorySlug("open")
            .Build();

        HttpResponseMessage addRawParticipantResponse = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{_rawMeetId}/participants",
            rawParticipantCommand,
            CancellationToken.None);

        addRawParticipantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? rawParticipantResult = await addRawParticipantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int rawParticipationId = rawParticipantResult!.ParticipationId;

        // Record attempts on raw meet — triggers record computation for classic records
        await RecordAttempt(_rawMeetId, rawParticipationId, (int)Discipline.Bench, 1, BenchWeight);
        await RecordAttempt(_rawMeetId, rawParticipationId, (int)Discipline.Deadlift, 1, DeadliftWeight);
        await RecordAttempt(_rawMeetId, rawParticipationId, (int)Discipline.Squat, 1, ClassicSquatWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Add participant to equipped meet: 83kg weight class, open age
        AddParticipantCommand equippedParticipantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(_athleteSlug)
            .WithBodyWeight(82.5m)
            .WithAgeCategorySlug("open")
            .Build();

        HttpResponseMessage addEquippedParticipantResponse = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{_equippedMeetId}/participants",
            equippedParticipantCommand,
            CancellationToken.None);

        addEquippedParticipantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? equippedParticipantResult = await addEquippedParticipantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int equippedParticipationId = equippedParticipantResult!.ParticipationId;

        // Record attempts on equipped meet — triggers record computation for equipped records
        await RecordAttempt(_equippedMeetId, equippedParticipationId, (int)Discipline.Bench, 1, BenchWeight);
        await RecordAttempt(_equippedMeetId, equippedParticipationId, (int)Discipline.Deadlift, 1, DeadliftWeight);
        await RecordAttempt(
            _equippedMeetId, equippedParticipationId, (int)Discipline.Squat, 1, EquippedSquatWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Find a squat attempt ID from the raw meet for negative-filter SQL INSERTs
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int squatAttemptId = await dbContext.Set<WebApi.Features.Attempts.Attempt>()
            .Where(a => a.ParticipationId == rawParticipationId)
            .Where(a => a.Discipline == Discipline.Squat)
            .Select(a => a.AttemptId)
            .SingleAsync(CancellationToken.None);

        // SQL INSERTs below are for record types that computation does not produce.
        // These records exist in the DB but should be filtered out by the handler.

        // Standard record — no endpoint produces IsStandard=true records
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES
                ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 1, {StandardSquatWeight}, GETUTCDATE(), 1, {squatAttemptId}, 1, 1, 'test-setup')
            """);

        // TotalWilks record — computation does not produce TotalWilks category
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES
                ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 7, {TotalWilksWeight}, GETUTCDATE(), 0, {squatAttemptId}, 1, 1, 'test-setup')
            """);

        // TotalIpfPoints record — computation does not produce TotalIpfPoints category
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES
                ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 8, {TotalIpfPointsWeight}, GETUTCDATE(), 0, {squatAttemptId}, 1, 1, 'test-setup')
            """);
    }

    public async ValueTask DisposeAsync()
    {
        if (_rawMeetId != 0 || _equippedMeetId != 0)
        {
            await CleanupTestDataAsync();
        }

        _authorizedHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WithRecords_WhenMeetHasApprovedRecords()
    {
        // Arrange & Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{_rawMeetSlug}/records",
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
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{_rawMeetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        // Standard record is for 93kg squat (220kg) — exists in DB but should be filtered out
        records
            .Where(r => r.WeightCategory == "93")
            .Where(r => r.Discipline == "Hnébeygja")
            .ShouldNotContain(r => r.Weight == StandardSquatWeight);
    }

    [Fact]
    public async Task DoesNotInclude_TotalWilksRecords()
    {
        // Arrange & Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{_rawMeetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        // TotalWilks record exists in DB but should be filtered out
        records.ShouldNotContain(r => r.Weight == TotalWilksWeight);
    }

    [Fact]
    public async Task DoesNotInclude_TotalIpfPointsRecords()
    {
        // Arrange & Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{_rawMeetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        // TotalIpfPoints record exists in DB but should be filtered out
        records.ShouldNotContain(r => r.Weight == TotalIpfPointsWeight);
    }

    [Fact]
    public async Task IncludesCorrectFields_ForEquippedRecord()
    {
        // Arrange & Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{_equippedMeetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        MeetRecordEntry squatRecord = records
            .Where(r => r.Weight == EquippedSquatWeight)
            .Where(r => r.Discipline == "Hnébeygja")
            .Where(r => r.AgeCategory == "Open")
            .First(r => !r.IsClassic);

        squatRecord.AthleteName.ShouldBe(_athleteName);
        squatRecord.AthleteSlug.ShouldBe(_athleteSlug);
        squatRecord.WeightCategory.ShouldBe("83");
        squatRecord.IsClassic.ShouldBeFalse();
    }

    [Fact]
    public async Task IncludesClassicRecords_WithIsClassicTrue()
    {
        // Arrange & Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{_rawMeetSlug}/records",
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
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/non-existent-meet/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IsPublic_NoAuthRequired()
    {
        // Arrange -- using unauthenticated client
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{_rawMeetSlug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task RecordAttempt(
        int meetId,
        int participationId,
        int disciplineId,
        int round,
        decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(true)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{disciplineId}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task CleanupTestDataAsync()
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<int> meetIds = new[] { _rawMeetId, _equippedMeetId }
            .Where(id => id != 0)
            .ToList();

        string meetIdList = string.Join(", ", meetIds);

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
            DELETE FROM Meets WHERE MeetId IN ({meetIdList});
            """;

        await dbContext.Database.ExecuteSqlRawAsync(cleanupSql);

        if (_athleteSlug.Length > 0)
        {
            await _authorizedHttpClient.DeleteAsync(
                $"/athletes/{_athleteSlug}",
                CancellationToken.None);
        }
    }
}