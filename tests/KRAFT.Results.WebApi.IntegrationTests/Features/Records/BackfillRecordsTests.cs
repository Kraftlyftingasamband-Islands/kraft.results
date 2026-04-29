using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using RecordEntity = KRAFT.Results.WebApi.Features.Records.Record;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(BackfillRecordsTestsCollection))]
public sealed class BackfillRecordsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const int NorwayCountryId = 2;
    private const int DeadliftMeetTypeId = 3;

    // Body weights used when adding participants
    private const decimal OpenBodyWeight = 90.0m;
    private const decimal HeavyBodyWeight = 103.0m;
    private const decimal LightBodyWeight = 80.5m;

    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];

    // Shared meet IDs populated in InitializeAsync
    private int _rawMeetId;
    private int _deadliftMeetId;

    // Base participation ID (anchor for corruption records)
    private int _baseParticipationId;

    // Base attempt ID for bench — used to anchor corruption records
    private int _baseBenchAttemptId;

    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        // Raw/classic powerlifting meet
        _rawMeetId = await CreateMeetAndGetIdAsync(isRaw: true);

        // Raw deadlift-only meet (MeetTypeId=3)
        _deadliftMeetId = await CreateMeetAndGetIdAsync(isRaw: true, meetTypeId: DeadliftMeetTypeId);

        // Base athlete: Icelandic, born 2003-01-01 (junior eligible)
        string baseAthleteSlug = await CreateAthleteAsync("BkfBase", "m", new DateOnly(2003, 1, 1));

        // Base participation + all three attempts — anchors the corruption records seeded below.
        _baseParticipationId = await AddParticipantAsync(_rawMeetId, baseAthleteSlug, OpenBodyWeight);
        await RecordAttemptAsync(_rawMeetId, _baseParticipationId, Discipline.Squat, 1, 150.0m);
        await RecordAttemptAsync(_rawMeetId, _baseParticipationId, Discipline.Bench, 1, 100.0m);
        await RecordAttemptAsync(_rawMeetId, _baseParticipationId, Discipline.Deadlift, 1, 180.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope idScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext idDb = idScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        _baseBenchAttemptId = await GetAttemptIdByRoundAsync(
            idDb, _baseParticipationId, Discipline.Bench, 1, TestContext.Current.CancellationToken);

        // Standard record (equipped/masters4/93kg/squat, IsStandard=1).
        // Endpoints cannot create standard records — SQL is required.
        await InsertStandardRecordIfAbsentAsync();

        // Corruption records referencing the base bench attempt.
        await InsertCorruptionRecordsAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // Delete per-test participations (all except the base participation)
        List<(int MeetId, int ParticipationId)> testParticipations = _participations
            .Where(p => p.ParticipationId != _baseParticipationId)
            .ToList();

        foreach ((int meetId, int participationId) in testParticipations)
        {
            await _authorizedHttpClient.DeleteAsync(
                $"/meets/{meetId}/participants/{participationId}", CancellationToken.None);
        }

        // Remove SQL-only state that endpoints cannot delete
        await CleanupSqlOnlyStateAsync();

        // Delete base participation via endpoint (cascades to attempts and records)
        await _authorizedHttpClient.DeleteAsync(
            $"/meets/{_rawMeetId}/participants/{_baseParticipationId}", CancellationToken.None);

        foreach (string slug in _meetSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{slug}", CancellationToken.None);
        }

        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task WhenBackfillRuns_RecordChainIsCorrect()
    {
        // Arrange — clear the junior/93kg/squat/raw slot, seed two squat attempts (180kg and 220kg)
        // and two orphan records with null AttemptId. After backfill the orphans are deleted and
        // the chain is 180kg then 220kg as current.
        await ResetRecordSlotAsync(
            TestSeedConstants.AgeCategory.JuniorId,
            TestSeedConstants.WeightCategory.Id93Kg);

        string athleteSlug = await CreateAthleteAsync("BkfChain", "m", new DateOnly(2003, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, OpenBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 180.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 2, 220.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope idScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext idDb = idScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int highAttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Squat, 2, TestContext.Current.CancellationToken);

        // Orphan records (AttemptId=NULL) — endpoints cannot produce these; SQL required.
        await InsertOrphanRecordsAsync(
            TestSeedConstants.AgeCategory.JuniorId,
            TestSeedConstants.WeightCategory.Id93Kg);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.JuniorId)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id93Kg)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.IsRaw)
            .ToListAsync(CancellationToken.None);

        slotRecords.Count(r => r.IsCurrent).ShouldBe(1, "exactly one record should be current");

        RecordEntity currentRecord = slotRecords.Single(r => r.IsCurrent);
        currentRecord.Weight.ShouldBe(220.0m);
        currentRecord.AttemptId.ShouldBe(highAttemptId);

        slotRecords.ShouldNotContain(r => r.AttemptId == null, "orphan records should be deleted");
        slotRecords.ShouldNotContain(r => r.Weight == 160.0m, "corrupt 160kg orphan should be deleted");
    }

    [Fact]
    public async Task WhenBackfillRunsTwice_ResultIsIdempotent()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        // Act — run twice with separate instances (BackgroundService only executes once per instance)
        using (BackfillRecordsJob firstRun = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance))
        {
            await firstRun.StartAsync(CancellationToken.None);
            await (firstRun.ExecuteTask ?? Task.CompletedTask);
        }

        using (BackfillRecordsJob secondRun = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance))
        {
            await secondRun.StartAsync(CancellationToken.None);
            await (secondRun.ExecuteTask ?? Task.CompletedTask);
        }

        // Assert — each slot should have exactly one current record
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> allCurrentRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        IEnumerable<IGrouping<string, RecordEntity>> slots = allCurrentRecords
            .GroupBy(r => $"{r.EraId}-{r.AgeCategoryId}-{r.WeightCategoryId}-{r.RecordCategoryId}-{r.IsRaw}");

        foreach (IGrouping<string, RecordEntity> slot in slots)
        {
            slot.Count().ShouldBe(1, $"slot {slot.Key} should have exactly one current record");
        }
    }

    [Fact]
    public async Task WhenBackfillRuns_TotalRecordIsCreated()
    {
        // Arrange — athlete with all three disciplines gives a valid total; clear total records
        // so backfill re-creates them from the attempts. Squat=220, Bench=140, Deadlift=260, Total=620.
        await ResetRecordSlotAsync(
            TestSeedConstants.AgeCategory.JuniorId,
            TestSeedConstants.WeightCategory.Id93Kg);

        string athleteSlug = await CreateAthleteAsync("BkfTotal", "m", new DateOnly(2003, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, OpenBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 220.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope clearScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext clearDb = clearScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        string deleteTotalSql =
            $"""
            DELETE FROM Records
            WHERE EraId = {TestSeedConstants.Era.CurrentId}
            AND AgeCategoryId = {TestSeedConstants.AgeCategory.JuniorId}
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id93Kg}
            AND RecordCategoryId = {(int)RecordCategory.Total}
            AND IsRaw = 1;
            """;

        await clearDb.Database.ExecuteSqlRawAsync(
            deleteTotalSql, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> totalRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.JuniorId)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id93Kg)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .Where(r => r.IsRaw)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        totalRecords.Count.ShouldBe(1);
        totalRecords[0].Weight.ShouldBe(620.0m);
    }

    [Fact]
    public async Task WhenBackfillRuns_DeadliftSingleRecordIsCreated()
    {
        // Arrange — a deadlift attempt at a deadlift-only meet should create a DeadliftSingle record.
        await ResetRecordSlotAsync(
            TestSeedConstants.AgeCategory.OpenId,
            TestSeedConstants.WeightCategory.Id83Kg,
            recordCategory: RecordCategory.DeadliftSingle);

        string athleteSlug = await CreateAthleteAsync("BkfDlSgl", "m", new DateOnly(1985, 7, 2));
        int participationId = await AddParticipantAsync(_deadliftMeetId, athleteSlug, LightBodyWeight);

        await RecordAttemptAsync(_deadliftMeetId, participationId, Discipline.Deadlift, 1, 280.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Clear the slot so backfill re-creates the record
        await using AsyncServiceScope clearScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext clearDb = clearScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        string deleteDlSingleSql =
            $"""
            DELETE FROM Records
            WHERE EraId = {TestSeedConstants.Era.CurrentId}
            AND AgeCategoryId = {TestSeedConstants.AgeCategory.OpenId}
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg}
            AND RecordCategoryId = {(int)RecordCategory.DeadliftSingle}
            AND IsRaw = 1;
            """;

        await clearDb.Database.ExecuteSqlRawAsync(
            deleteDlSingleSql, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> dlSingleRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.OpenId)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.RecordCategoryId == RecordCategory.DeadliftSingle)
            .Where(r => r.IsRaw)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        dlSingleRecords.Count.ShouldBe(1);
        dlSingleRecords[0].Weight.ShouldBe(280.0m);
    }

    [Fact]
    public async Task WhenBackfillRuns_StandardRecordIsNotDeleted()
    {
        // Arrange — a standard record (IsStandard=1) in the equipped/masters4/93kg/squat slot
        // is seeded in InitializeAsync. The slot has no athlete attempt, so the expected
        // chain is empty. The buggy code deleted standard records in this situation.
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — the standard record in the equipped masters4 93kg squat slot must survive
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.Masters4Id)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id93Kg)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => !r.IsRaw)
            .ToListAsync(CancellationToken.None);

        slotRecords.ShouldContain(r => r.IsStandard, "standard record should not be deleted by backfill");
    }

    [Fact]
    public async Task WhenNonIcelandicAthleteCompetes_BackfillDoesNotCreateRecord()
    {
        // Arrange
        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await ResetRecordSlotAsync(TestSeedConstants.AgeCategory.Masters4Id, weightCategoryId);

        string norwegianSlug = await CreateAthleteAsync(
            "BkfNor", "m", new DateOnly(1950, 1, 1), countryId: NorwayCountryId);
        int participationId = await AddParticipantAsync(_rawMeetId, norwegianSlug, HeavyBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 300.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 350.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope idScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext idDb = idScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int squatAttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Squat, 1, TestContext.Current.CancellationToken);
        int benchAttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Bench, 1, TestContext.Current.CancellationToken);
        int deadliftAttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Deadlift, 1, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — no records should reference the Norwegian athlete's attempts
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.AttemptId == squatAttemptId
                || r.AttemptId == benchAttemptId
                || r.AttemptId == deadliftAttemptId)
            .ToListAsync(CancellationToken.None);

        slotRecords.ShouldBeEmpty("non-Icelandic athletes should not get records via backfill");
    }

    [Fact]
    public async Task WhenBackfillRuns_BenchAndDeadliftFromPowerliftingMeetAlsoCreateSingleLiftRecords()
    {
        // Arrange — a bench attempt from a powerlifting meet should create Bench (Cat=2) and
        // BenchSingle (Cat=5) records. Likewise a deadlift creates Deadlift (Cat=3) and
        // DeadliftSingle (Cat=6) records.
        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await ResetRecordSlotAsync(TestSeedConstants.AgeCategory.Masters4Id, weightCategoryId);

        string athleteSlug = await CreateAthleteAsync("BkfSglLft", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, HeavyBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope idScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext idDb = idScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int benchAttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Bench, 1, TestContext.Current.CancellationToken);
        int deadliftAttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Deadlift, 1, TestContext.Current.CancellationToken);

        // Clear single-lift slots so backfill re-creates them
        string deleteSingleLiftSql =
            $"""
            DELETE FROM Records
            WHERE EraId = {TestSeedConstants.Era.CurrentId}
            AND AgeCategoryId = {TestSeedConstants.AgeCategory.Masters4Id}
            AND WeightCategoryId = {weightCategoryId}
            AND RecordCategoryId IN (
                {(int)RecordCategory.Bench}, {(int)RecordCategory.BenchSingle},
                {(int)RecordCategory.Deadlift}, {(int)RecordCategory.DeadliftSingle})
            AND IsRaw = 1;
            """;

        await idDb.Database.ExecuteSqlRawAsync(
            deleteSingleLiftSql, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        RecordEntity? benchRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == benchAttemptId)
            .FirstOrDefaultAsync(r => r.RecordCategoryId == RecordCategory.Bench, CancellationToken.None);

        RecordEntity? benchSingleRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == benchAttemptId)
            .FirstOrDefaultAsync(r => r.RecordCategoryId == RecordCategory.BenchSingle, CancellationToken.None);

        benchRecord.ShouldNotBeNull("bench from powerlifting meet should produce a Bench record");
        benchRecord.Weight.ShouldBe(130m);

        benchSingleRecord.ShouldNotBeNull(
            "bench from powerlifting meet should also produce a BenchSingle record");
        benchSingleRecord.Weight.ShouldBe(130m);

        RecordEntity? deadliftRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == deadliftAttemptId)
            .FirstOrDefaultAsync(r => r.RecordCategoryId == RecordCategory.Deadlift, CancellationToken.None);

        RecordEntity? deadliftSingleRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == deadliftAttemptId)
            .FirstOrDefaultAsync(r => r.RecordCategoryId == RecordCategory.DeadliftSingle, CancellationToken.None);

        deadliftRecord.ShouldNotBeNull("deadlift from powerlifting meet should produce a Deadlift record");
        deadliftRecord.Weight.ShouldBe(250m);

        deadliftSingleRecord.ShouldNotBeNull(
            "deadlift from powerlifting meet should also produce a DeadliftSingle record");
        deadliftSingleRecord.Weight.ShouldBe(250m);
    }

    [Fact]
    public async Task WhenBackfillRuns_AttemptWithLowerIdButHigherWeightDoesNotSuppressRecord()
    {
        // Arrange — two athletes in the same slot (masters4 / 93kg / squat / raw), lifting 140kg
        // and 110kg respectively. Both should appear in the chain regardless of insertion order.
        // The buggy sort (by AttemptId ascending) could suppress the lower-weight record if the
        // higher-weight attempt happened to have the lower AttemptId.
        await ResetRecordSlotAsync(
            TestSeedConstants.AgeCategory.Masters4Id,
            TestSeedConstants.WeightCategory.Id93Kg);

        string athlete1Slug = await CreateAthleteAsync("BkfSort1", "m", new DateOnly(1950, 1, 1));
        int participation1Id = await AddParticipantAsync(_rawMeetId, athlete1Slug, OpenBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participation1Id, Discipline.Squat, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participation1Id, Discipline.Bench, 1, 100.0m);
        await RecordAttemptAsync(_rawMeetId, participation1Id, Discipline.Deadlift, 1, 150.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        string athlete2Slug = await CreateAthleteAsync("BkfSort2", "m", new DateOnly(1950, 1, 1));
        int participation2Id = await AddParticipantAsync(_rawMeetId, athlete2Slug, OpenBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participation2Id, Discipline.Squat, 1, 110.0m);
        await RecordAttemptAsync(_rawMeetId, participation2Id, Discipline.Bench, 1, 80.0m);
        await RecordAttemptAsync(_rawMeetId, participation2Id, Discipline.Deadlift, 1, 130.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope idScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext idDb = idScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int squat1AttemptId = await GetAttemptIdByRoundAsync(
            idDb, participation1Id, Discipline.Squat, 1, TestContext.Current.CancellationToken);
        int squat2AttemptId = await GetAttemptIdByRoundAsync(
            idDb, participation2Id, Discipline.Squat, 1, TestContext.Current.CancellationToken);

        // Clear the slot so backfill rebuilds from scratch
        await SeedRecordAthlete.ClearSlotAsync(
            idDb, TestSeedConstants.WeightCategory.Id93Kg, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — both records must appear in the chain
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.Masters4Id)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id93Kg)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.IsRaw)
            .ToListAsync(CancellationToken.None);

        slotRecords.ShouldContain(
            r => r.AttemptId == squat1AttemptId,
            "higher-weight attempt should be in chain");

        slotRecords.ShouldContain(
            r => r.AttemptId == squat2AttemptId,
            "lower-weight attempt should not be suppressed by sort order");

        RecordEntity currentRecord = slotRecords.Single(r => r.IsCurrent);
        currentRecord.AttemptId.ShouldBe(squat1AttemptId, "current record should be the highest weight");
        currentRecord.Weight.ShouldBe(140m);
    }

    [Fact]
    public async Task WhenStandardRecordExists_ChainStartsFromStandardWeight()
    {
        // Arrange — standard record at 250kg. Athlete A lifts 240kg which is below the standard
        // so no record is expected. Athlete B lifts 260kg which is above the standard
        // and should produce exactly one record.
        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await ResetRecordSlotAsync(TestSeedConstants.AgeCategory.Masters4Id, weightCategoryId);

        // Endpoints cannot create standard records — SQL is required.
        await using AsyncServiceScope insertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext insertDb = insertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        string insertStandardSql =
            $"""
            INSERT INTO Records (
                EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (
                {TestSeedConstants.Era.CurrentId},
                {TestSeedConstants.AgeCategory.Masters4Id},
                {weightCategoryId},
                {(int)RecordCategory.Squat},
                250.0, '2020-06-01', 1, NULL, 1, 1, 'backfill-test');
            """;

        await insertDb.Database.ExecuteSqlRawAsync(
            insertStandardSql, TestContext.Current.CancellationToken);

        // Athlete A: squat=240 (below standard — should NOT produce a record)
        string athleteASlug = await CreateAthleteAsync("BkfStdA", "m", new DateOnly(1950, 1, 1));
        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, HeavyBodyWeight);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 240.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Athlete B: squat=260 (above standard — SHOULD produce a record)
        string athleteBSlug = await CreateAthleteAsync("BkfStdB", "m", new DateOnly(1950, 1, 1));
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, HeavyBodyWeight);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 260.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Clear non-standard records so backfill re-evaluates from the standard weight
        string deleteNonStandardSql =
            $"""
            DELETE FROM Records
            WHERE EraId = {TestSeedConstants.Era.CurrentId}
            AND AgeCategoryId = {TestSeedConstants.AgeCategory.Masters4Id}
            AND WeightCategoryId = {weightCategoryId}
            AND RecordCategoryId = {(int)RecordCategory.Squat}
            AND IsRaw = 1
            AND IsStandard = 0;
            """;

        await insertDb.Database.ExecuteSqlRawAsync(
            deleteNonStandardSql, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.Masters4Id)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.IsRaw)
            .ToListAsync(CancellationToken.None);

        slotRecords.ShouldContain(r => r.IsStandard, "standard record should still exist after backfill");

        List<RecordEntity> nonStandardRecords = slotRecords
            .Where(r => !r.IsStandard)
            .ToList();

        nonStandardRecords.Count.ShouldBe(1, "only one non-standard squat record should exist");
        nonStandardRecords[0].Weight.ShouldBe(260.0m);
        nonStandardRecords[0].IsCurrent.ShouldBeTrue();

        slotRecords.ShouldNotContain(
            r => r.Weight == 240.0m,
            "attempt below standard weight should not produce a record");
    }

    [Fact]
    public async Task WhenMultipleGoodDeadlifts_IntermediateTotalRecordsAreCreated()
    {
        // Arrange — athlete has squat + bench + three improving deadlifts (rounds 1, 2, 3).
        // Each improving deadlift produces a new best total; backfill should create one Total
        // record per improving deadlift: 580 (dl=250), 590 (dl=260), 600 (dl=270, current).
        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await ResetRecordSlotAsync(TestSeedConstants.AgeCategory.Masters4Id, weightCategoryId);

        string athleteSlug = await CreateAthleteAsync("BkfIntTot", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, HeavyBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Rounds 2 and 3 for deadlift (endpoint supports all three rounds)
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 2, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 3, 270.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope idScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext idDb = idScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int dl1AttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Deadlift, 1, TestContext.Current.CancellationToken);
        int dl2AttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Deadlift, 2, TestContext.Current.CancellationToken);
        int dl3AttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Deadlift, 3, TestContext.Current.CancellationToken);

        // Clear total records so backfill re-creates all three
        string deleteTotalSql =
            $"""
            DELETE FROM Records
            WHERE EraId = {TestSeedConstants.Era.CurrentId}
            AND AgeCategoryId = {TestSeedConstants.AgeCategory.Masters4Id}
            AND WeightCategoryId = {weightCategoryId}
            AND RecordCategoryId = {(int)RecordCategory.Total}
            AND IsRaw = 1;
            """;

        await idDb.Database.ExecuteSqlRawAsync(
            deleteTotalSql, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> totalRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.Masters4Id)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .Where(r => r.IsRaw)
            .OrderBy(r => r.Weight)
            .ToListAsync(CancellationToken.None);

        totalRecords.Count.ShouldBe(3, "one total record per improving deadlift attempt");

        totalRecords[0].Weight.ShouldBe(580.0m);
        totalRecords[0].AttemptId.ShouldBe(dl1AttemptId);
        totalRecords[0].IsCurrent.ShouldBeFalse();

        totalRecords[1].Weight.ShouldBe(590.0m);
        totalRecords[1].AttemptId.ShouldBe(dl2AttemptId);
        totalRecords[1].IsCurrent.ShouldBeFalse();

        totalRecords[2].Weight.ShouldBe(600.0m);
        totalRecords[2].AttemptId.ShouldBe(dl3AttemptId);
        totalRecords[2].IsCurrent.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenFourthAttemptExists_TotalExcludesRoundFourWeight()
    {
        // Arrange — standard squat/bench/deadlift via endpoints, plus a round-4 squat inserted
        // via SQL (the endpoint does not support round 4). Round 4 is not a valid competition
        // attempt for totals; the total should be 200+130+250=580, not 220+130+250=600.
        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await ResetRecordSlotAsync(TestSeedConstants.AgeCategory.Masters4Id, weightCategoryId);

        string athleteSlug = await CreateAthleteAsync("BkfR4", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, HeavyBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope sqlScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext sqlDb = sqlScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        // Round 4 squat: endpoint does not support round 4; SQL required to simulate
        // production data where extra-attempt records exist.
        string insertRound4Sql =
            $"""
            INSERT INTO Attempts (
                ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (
                {participationId}, 1, 4, 220.0, 1, 'backfill-test', 'backfill-test');
            """;

        await sqlDb.Database.ExecuteSqlRawAsync(
            insertRound4Sql, TestContext.Current.CancellationToken);

        // Clear total records so backfill re-creates
        string deleteTotalSql =
            $"""
            DELETE FROM Records
            WHERE EraId = {TestSeedConstants.Era.CurrentId}
            AND AgeCategoryId = {TestSeedConstants.AgeCategory.Masters4Id}
            AND WeightCategoryId = {weightCategoryId}
            AND RecordCategoryId = {(int)RecordCategory.Total}
            AND IsRaw = 1;
            """;

        await sqlDb.Database.ExecuteSqlRawAsync(
            deleteTotalSql, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> totalRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.Masters4Id)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .Where(r => r.IsRaw)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        totalRecords.Count.ShouldBe(1);
        totalRecords[0].Weight.ShouldBe(
            580.0m,
            "total should be 200+130+250=580, not 220+130+250=600");
    }

    [Fact]
    public async Task WhenDuplicateRecordsExistForSameAttempt_BackfillDeduplicates()
    {
        // Arrange
        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await ResetRecordSlotAsync(TestSeedConstants.AgeCategory.Masters4Id, weightCategoryId);

        string athleteSlug = await CreateAthleteAsync("BkfDup", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, HeavyBodyWeight);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope idScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext idDb = idScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int squatAttemptId = await GetAttemptIdByRoundAsync(
            idDb, participationId, Discipline.Squat, 1, TestContext.Current.CancellationToken);

        // Insert a duplicate record for the same attempt — endpoints prevent duplicates; SQL required
        // to simulate the corrupt production data that backfill must deduplicate.
        string insertDuplicateSql =
            $"""
            INSERT INTO Records (
                EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            SELECT
                EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, 'duplicate'
            FROM Records
            WHERE AttemptId = {squatAttemptId}
            AND RecordCategoryId = {(int)RecordCategory.Squat};
            """;

        await idDb.Database.ExecuteSqlRawAsync(
            insertDuplicateSql, TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> squatRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.Masters4Id)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.IsRaw)
            .Where(r => !r.IsStandard)
            .Where(r => r.AttemptId == squatAttemptId)
            .ToListAsync(CancellationToken.None);

        squatRecords.Count.ShouldBe(
            1,
            "duplicate records for the same attempt should be deduplicated");
    }

    private static async Task<int> GetAttemptIdByRoundAsync(
        ResultsDbContext dbContext,
        int participationId,
        Discipline discipline,
        int round,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Attempt>()
            .Where(a => a.ParticipationId == participationId)
            .Where(a => a.Discipline == discipline)
            .Where(a => a.Round == round)
            .Select(a => a.AttemptId)
            .SingleAsync(cancellationToken);
    }

    private async Task ResetRecordSlotAsync(
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory? recordCategory = null)
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        string deleteSql;

        if (recordCategory.HasValue)
        {
            deleteSql =
                $"""
                DELETE FROM Records
                WHERE EraId = {TestSeedConstants.Era.CurrentId}
                AND AgeCategoryId = {ageCategoryId}
                AND WeightCategoryId = {weightCategoryId}
                AND RecordCategoryId = {(int)recordCategory.Value}
                AND IsRaw = 1
                AND IsStandard = 0;
                """;
        }
        else
        {
            deleteSql =
                $"""
                DELETE FROM Records
                WHERE EraId = {TestSeedConstants.Era.CurrentId}
                AND AgeCategoryId = {ageCategoryId}
                AND WeightCategoryId = {weightCategoryId}
                AND IsRaw = 1
                AND IsStandard = 0;
                """;
        }

        await dbContext.Database.ExecuteSqlRawAsync(deleteSql, TestContext.Current.CancellationToken);
    }

    private async Task InsertStandardRecordIfAbsentAsync()
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        // Endpoints cannot create standard records — SQL is required.
        string sql =
            $"""
            IF NOT EXISTS (
                SELECT 1 FROM Records
                WHERE EraId = {TestSeedConstants.Era.CurrentId}
                AND AgeCategoryId = {TestSeedConstants.AgeCategory.Masters4Id}
                AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id93Kg}
                AND RecordCategoryId = {(int)RecordCategory.Squat}
                AND IsRaw = 0
                AND IsStandard = 1
                AND CreatedBy = 'backfill-test')
            BEGIN
                INSERT INTO Records (
                    EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                    Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
                VALUES (
                    {TestSeedConstants.Era.CurrentId},
                    {TestSeedConstants.AgeCategory.Masters4Id},
                    {TestSeedConstants.WeightCategory.Id93Kg},
                    {(int)RecordCategory.Squat},
                    220.0, '2025-01-01', 1, NULL, 1, 0, 'backfill-test');
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);
    }

    private async Task InsertCorruptionRecordsAsync()
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        // Corruption records represent intentionally corrupt data states that endpoints cannot produce:
        // - Two Records rows for the same AttemptId in different states (IsCurrent=0, IsCurrent=1)
        // - A BenchSingle record with IsStandard=1
        // Guard prevents double-insertion on test reruns.
        string sql =
            $"""
            IF NOT EXISTS (
                SELECT 1 FROM Records
                WHERE AttemptId = {_baseBenchAttemptId}
                AND RecordCategoryId = {(int)RecordCategory.Bench}
                AND Weight = 150.0
                AND CreatedBy = 'backfill-test')
            BEGIN
                INSERT INTO Records (
                    EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                    Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
                VALUES (
                    {TestSeedConstants.Era.CurrentId},
                    {TestSeedConstants.AgeCategory.Masters4Id},
                    {TestSeedConstants.WeightCategory.Id93Kg},
                    {(int)RecordCategory.Bench},
                    150.0, '2025-06-01', 0, {_baseBenchAttemptId}, 0, 0, 'backfill-test');

                INSERT INTO Records (
                    EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                    Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
                VALUES (
                    {TestSeedConstants.Era.CurrentId},
                    {TestSeedConstants.AgeCategory.Masters4Id},
                    {TestSeedConstants.WeightCategory.Id93Kg},
                    {(int)RecordCategory.Bench},
                    140.0, '2025-05-01', 0, {_baseBenchAttemptId}, 1, 0, 'backfill-test');

                INSERT INTO Records (
                    EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                    Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
                VALUES (
                    {TestSeedConstants.Era.CurrentId},
                    {TestSeedConstants.AgeCategory.Masters4Id},
                    {TestSeedConstants.WeightCategory.Id83Kg},
                    {(int)RecordCategory.BenchSingle},
                    130.0, '2025-03-15', 1, {_baseBenchAttemptId}, 1, 0, 'backfill-test');
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);
    }

    private async Task InsertOrphanRecordsAsync(int ageCategoryId, int weightCategoryId)
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        // Orphan records have AttemptId=NULL — endpoints never produce this state.
        // SQL is required to simulate corrupt production data that backfill must delete.
        string sql =
            $"""
            INSERT INTO Records (
                EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (
                {TestSeedConstants.Era.CurrentId},
                {ageCategoryId},
                {weightCategoryId},
                {(int)RecordCategory.Squat},
                150.0, '2025-01-01', 0, NULL, 1, 1, 'backfill-test');

            INSERT INTO Records (
                EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (
                {TestSeedConstants.Era.CurrentId},
                {ageCategoryId},
                {weightCategoryId},
                {(int)RecordCategory.Squat},
                160.0, '2025-02-01', 0, NULL, 1, 1, 'backfill-test');
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);
    }

    private async Task CleanupSqlOnlyStateAsync()
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM Records WHERE CreatedBy = 'backfill-test';",
            TestContext.Current.CancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM Attempts WHERE Round = 4 AND CreatedBy = 'backfill-test';",
            TestContext.Current.CancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM Records WHERE CreatedBy = 'duplicate';",
            TestContext.Current.CancellationToken);
    }

    private async Task<string> CreateAthleteAsync(
        string prefix, string gender, DateOnly dateOfBirth, int countryId = 1)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Bf";

        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender(gender)
            .WithDateOfBirth(dateOfBirth)
            .WithCountryId(countryId)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{firstName} {lastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<int> CreateMeetAndGetIdAsync(bool isRaw, int? meetTypeId = null)
    {
        CreateMeetCommandBuilder builder = new CreateMeetCommandBuilder()
            .WithIsRaw(isRaw)
            .WithRecordsPossible(true)
            .WithStartDate(new DateOnly(2025, 3, 15));

        if (meetTypeId.HasValue)
        {
            builder.WithMeetTypeId(meetTypeId.Value);
        }

        CreateMeetCommand command = builder.Build();

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

        int participationId = result!.ParticipationId;
        _participations.Add((meetId, participationId));
        return participationId;
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