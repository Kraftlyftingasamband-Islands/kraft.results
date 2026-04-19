using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using RecordEntity = KRAFT.Results.WebApi.Features.Records.Record;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(RecordsCollection))]
public sealed class BackfillRecordsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const int BackfillTestParticipationId = 500;
    private const int BackfillTestAttemptLowId = 500;
    private const int BackfillTestAttemptHighId = 501;
    private const int BackfillTestBenchAttemptId = 502;
    private const int BackfillTestDeadliftAttemptId = 503;
    private const int DeadliftMeetParticipationId = 600;
    private const int DeadliftMeetAttemptId = 600;
    private const int NonIcelandicAthleteBaseId = 700;

    // Two athletes whose squat AttemptIds are inversely ordered relative to their weights,
    // mirroring production data where records were entered in descending weight order.
    // Athlete1 (baseId=800): squatAttemptId=800, squat=140 kg (lower ID, higher weight).
    // Athlete2 (baseId=803): squatAttemptId=803, squat=110 kg (higher ID, lower weight).
    // With the buggy sort (by AttemptId), 800 (140 kg) is processed first and becomes
    // runningMax; 803 (110 kg < 140 kg) is skipped and its record would be created fresh
    // only if it appears — but since 110 < 140 it never enters the chain, so no record.
    // With the fix (sort by weight first), 803 (110 kg) comes before 800 (140 kg),
    // both enter the chain, and both records are created.
    private const int SortOrderAthlete1BaseId = 800; // squatAttemptId=800, 140 kg
    private const int SortOrderAthlete2BaseId = 803; // squatAttemptId=803, 110 kg

    private const int SingleLiftAthleteBaseId = 900;
    private const int StandardRecordAthleteABaseId = 1000;
    private const int StandardRecordAthleteBBaseId = 1003;
    private const int IntermediateTotalAthleteBaseId = 1010;
    private const int IntermediateTotalDlRound2AttemptId = 1013;
    private const int IntermediateTotalDlRound3AttemptId = 1014;
    private const int FourthAttemptAthleteBaseId = 1020;
    private const int FourthAttemptRound4AttemptId = 1024;
    private const int DuplicateRecordAthleteBaseId = 1030;

    private const int NorwayCountryId = 2;
    private const string SeedAthleteDateOfBirth = "1985-07-02";

    private const string SeedRecordCorruptionSql =
        """
        INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (2, 1, 2, 2, 150.0, '2025-06-01', 0, 2, 0, 0, 'seed');

        INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (2, 1, 2, 2, 140.0, '2025-05-01', 0, 2, 1, 0, 'seed');

        INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (2, 1, 1, 5, 130.0, '2025-03-15', 1, 2, 1, 0, 'seed');
        """;

    [Fact]
    public async Task WhenBackfillRuns_RecordChainIsCorrect()
    {
        // Arrange
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedBackfillTestDataAsync(dbContext);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — slot: era=2, ageCategory=junior(2), weightCategory=93kg(2), squat(1), isRaw=true.
        // Expected chain: attempt 500 (180kg) -> attempt 501 (220kg, current).
        // The orphan seed record at 150kg (no attempt) should be deleted.
        // The corrupt record at 160kg (no matching attempt) should be deleted.
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
        currentRecord.AttemptId.ShouldBe(BackfillTestAttemptHighId);

        // The orphan seed records (AttemptId = null) should have been deleted
        slotRecords.ShouldNotContain(r => r.AttemptId == null);

        // The corrupt 160kg record should have been deleted
        slotRecords.ShouldNotContain(r => r.Weight == 160.0m);
    }

    [Fact]
    public async Task WhenBackfillRunsTwice_ResultIsIdempotent()
    {
        // Arrange
        await ResetToBaseStateAsync();

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

        // Assert — the record chain should be consistent after two runs
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> allCurrentRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        // Group by slot and verify each slot has exactly one current record
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
        // Arrange
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedBackfillTotalTestDataAsync(dbContext);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — Total record should exist for the participation with Total=620
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
        // Arrange
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedDeadliftMeetBackfillDataAsync(dbContext);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — DeadliftSingle record should exist
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
        // Arrange — base seed includes a standard record (RecordId=6) in the
        // equipped / open / 93 kg / squat slot with no athlete attempts.
        // The expected chain for that slot is empty, so the buggy code deletes it.
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — the standard record in the 93 kg equipped open squat slot must survive
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.OpenId)
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
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, CancellationToken.None);

        SeedRecordAthlete norwegianAthlete = await new RecordTestAthleteBuilder(dbContext, NonIcelandicAthleteBaseId)
            .WithCountryId(NorwayCountryId)
            .WithWeightCategoryId(weightCategoryId)
            .WithSquat(300m)
            .WithBench(200m)
            .WithDeadlift(350m)
            .BuildAsync(CancellationToken.None);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — no records should exist for the non-Icelandic athlete's slot
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.Masters4Id)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.IsRaw)
            .Where(r => r.AttemptId == norwegianAthlete.SquatAttemptId
                || r.AttemptId == norwegianAthlete.BenchAttemptId
                || r.AttemptId == norwegianAthlete.DeadliftAttemptId)
            .ToListAsync(CancellationToken.None);

        slotRecords.ShouldBeEmpty("non-Icelandic athletes should not get records via backfill");
    }

    [Fact]
    public async Task WhenBackfillRuns_BenchAndDeadliftFromPowerliftingMeetAlsoCreateSingleLiftRecords()
    {
        // Arrange — a bench attempt from a powerlifting meet should create BOTH a Bench
        // (Cat=2) record AND a BenchSingle (Cat=5) record, mirroring production data where
        // both slots tracked lifts from full powerlifting competitions.
        // Likewise, a deadlift attempt should produce both Deadlift (Cat=3) and
        // DeadliftSingle (Cat=6) records.
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        SeedRecordAthlete athlete = await new RecordTestAthleteBuilder(dbContext, SingleLiftAthleteBaseId)
            .WithWeightCategoryId(TestSeedConstants.WeightCategory.Id105Kg)
            .WithSquat(200m)
            .WithBench(130m)
            .WithDeadlift(250m)
            .BuildAsync(CancellationToken.None);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — both Bench and BenchSingle records should be created for the bench attempt.
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        RecordEntity? benchRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == athlete.BenchAttemptId)
            .Where(r => r.RecordCategoryId == RecordCategory.Bench)
            .FirstOrDefaultAsync(CancellationToken.None);

        RecordEntity? benchSingleRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == athlete.BenchAttemptId)
            .Where(r => r.RecordCategoryId == RecordCategory.BenchSingle)
            .FirstOrDefaultAsync(CancellationToken.None);

        benchRecord.ShouldNotBeNull("bench from powerlifting meet should produce a Bench record");
        benchRecord.Weight.ShouldBe(130m);

        benchSingleRecord.ShouldNotBeNull(
            "bench from powerlifting meet should also produce a BenchSingle record");
        benchSingleRecord.Weight.ShouldBe(130m);

        // Also verify Deadlift and DeadliftSingle records.
        RecordEntity? deadliftRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == athlete.DeadliftAttemptId)
            .Where(r => r.RecordCategoryId == RecordCategory.Deadlift)
            .FirstOrDefaultAsync(CancellationToken.None);

        RecordEntity? deadliftSingleRecord = await assertDb.Set<RecordEntity>()
            .Where(r => r.AttemptId == athlete.DeadliftAttemptId)
            .Where(r => r.RecordCategoryId == RecordCategory.DeadliftSingle)
            .FirstOrDefaultAsync(CancellationToken.None);

        deadliftRecord.ShouldNotBeNull("deadlift from powerlifting meet should produce a Deadlift record");
        deadliftRecord.Weight.ShouldBe(250m);

        deadliftSingleRecord.ShouldNotBeNull(
            "deadlift from powerlifting meet should also produce a DeadliftSingle record");
        deadliftSingleRecord.Weight.ShouldBe(250m);
    }

    [Fact]
    public async Task WhenBackfillRuns_AttemptWithLowerIdButHigherWeightDoesNotSuppressRecord()
    {
        // Arrange — two athletes in the same slot (open / 93 kg / squat / raw).
        // Athlete1 (baseId=800): squatAttemptId=800, squat=140 kg (lower ID, higher weight).
        // Athlete2 (baseId=803): squatAttemptId=803, squat=110 kg (higher ID, lower weight).
        // The lower AttemptId has the higher weight, mirroring production data where
        // records were entered in descending weight order.
        // With the buggy sort (by AttemptId), 800 (140 kg) is processed first and sets
        // runningMax=140; 803 (110 kg < 140 kg) is skipped and never enters the chain.
        // After the fix (sort by weight first), 803 (110 kg) is processed first, 800
        // (140 kg) second — both enter the chain and both records are created.
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedRecordAthlete.ClearSlotAsync(
            dbContext,
            TestSeedConstants.WeightCategory.Id93Kg,
            CancellationToken.None);

        SeedRecordAthlete athlete1 = await new RecordTestAthleteBuilder(dbContext, SortOrderAthlete1BaseId)
            .WithWeightCategoryId(TestSeedConstants.WeightCategory.Id93Kg)
            .WithSquat(140m)
            .WithBench(100m)
            .WithDeadlift(150m)
            .BuildAsync(CancellationToken.None);

        SeedRecordAthlete athlete2 = await new RecordTestAthleteBuilder(dbContext, SortOrderAthlete2BaseId)
            .WithWeightCategoryId(TestSeedConstants.WeightCategory.Id93Kg)
            .WithSquat(110m)
            .WithBench(80m)
            .WithDeadlift(130m)
            .BuildAsync(CancellationToken.None);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        // Act
        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — both squat records must be created; athlete1 (140 kg) is current,
        // athlete2 (110 kg) is the predecessor in the progressive chain.
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> slotRecords = await assertDb.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.OpenId)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id93Kg)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.IsRaw)
            .ToListAsync(CancellationToken.None);

        slotRecords.ShouldContain(
            r => r.AttemptId == athlete1.SquatAttemptId,
            "higher-weight attempt (lower ID) should be in chain");

        slotRecords.ShouldContain(
            r => r.AttemptId == athlete2.SquatAttemptId,
            "lower-weight attempt (higher ID) should not be suppressed by sort order bug");

        RecordEntity currentRecord = slotRecords.Single(r => r.IsCurrent);
        currentRecord.AttemptId.ShouldBe(athlete1.SquatAttemptId, "current record should be the highest weight");
        currentRecord.Weight.ShouldBe(140m);
    }

    [Fact]
    public async Task WhenStandardRecordExists_ChainStartsFromStandardWeight()
    {
        // Arrange
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, CancellationToken.None);

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
                250.0, '2020-06-01', 1, NULL, 1, 1, 'test');
            """;

        await dbContext.Database.ExecuteSqlRawAsync(
            insertStandardSql,
            TestContext.Current.CancellationToken);

        await new RecordTestAthleteBuilder(dbContext, StandardRecordAthleteABaseId)
            .WithWeightCategoryId(weightCategoryId)
            .WithSquat(240m)
            .BuildAsync(CancellationToken.None);

        await new RecordTestAthleteBuilder(dbContext, StandardRecordAthleteBBaseId)
            .WithWeightCategoryId(weightCategoryId)
            .WithSquat(260m)
            .BuildAsync(CancellationToken.None);

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

        slotRecords.ShouldContain(
            r => r.IsStandard,
            "standard record should still exist after backfill");

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
        // Arrange
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, CancellationToken.None);

        SeedRecordAthlete athlete = await new RecordTestAthleteBuilder(
                dbContext,
                IntermediateTotalAthleteBaseId)
            .WithWeightCategoryId(weightCategoryId)
            .WithSquat(200m)
            .WithBench(130m)
            .WithDeadlift(250m)
            .BuildAsync(CancellationToken.None);

        string extraDeadliftsSql =
            $"""
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (
                AttemptId, ParticipationId, DisciplineId, Round, Weight, Good,
                CreatedBy, ModifiedBy)
            VALUES (
                {IntermediateTotalDlRound2AttemptId},
                {athlete.ParticipationId}, 3, 2, 260.0, 1, 'test', 'test');
            INSERT INTO Attempts (
                AttemptId, ParticipationId, DisciplineId, Round, Weight, Good,
                CreatedBy, ModifiedBy)
            VALUES (
                {IntermediateTotalDlRound3AttemptId},
                {athlete.ParticipationId}, 3, 3, 270.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;

            UPDATE Participations
            SET Deadlift = 270.0, Total = 600.0
            WHERE ParticipationId = {athlete.ParticipationId};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(
            extraDeadliftsSql,
            TestContext.Current.CancellationToken);

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
        totalRecords[0].AttemptId.ShouldBe(athlete.DeadliftAttemptId);
        totalRecords[0].IsCurrent.ShouldBeFalse();

        totalRecords[1].Weight.ShouldBe(590.0m);
        totalRecords[1].AttemptId.ShouldBe(IntermediateTotalDlRound2AttemptId);
        totalRecords[1].IsCurrent.ShouldBeFalse();

        totalRecords[2].Weight.ShouldBe(600.0m);
        totalRecords[2].AttemptId.ShouldBe(IntermediateTotalDlRound3AttemptId);
        totalRecords[2].IsCurrent.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenFourthAttemptExists_TotalExcludesRoundFourWeight()
    {
        // Arrange
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, CancellationToken.None);

        SeedRecordAthlete athlete = await new RecordTestAthleteBuilder(
                dbContext,
                FourthAttemptAthleteBaseId)
            .WithWeightCategoryId(weightCategoryId)
            .WithSquat(200m)
            .WithBench(130m)
            .WithDeadlift(250m)
            .BuildAsync(CancellationToken.None);

        string round4SquatSql =
            $"""
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (
                AttemptId, ParticipationId, DisciplineId, Round, Weight, Good,
                CreatedBy, ModifiedBy)
            VALUES (
                {FourthAttemptRound4AttemptId},
                {athlete.ParticipationId}, 1, 4, 220.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(
            round4SquatSql,
            TestContext.Current.CancellationToken);

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
        await ResetToBaseStateAsync();

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, CancellationToken.None);

        SeedRecordAthlete athlete = await new RecordTestAthleteBuilder(
                dbContext,
                DuplicateRecordAthleteBaseId)
            .WithWeightCategoryId(weightCategoryId)
            .WithSquat(200m)
            .WithBench(130m)
            .WithDeadlift(250m)
            .BuildAsync(CancellationToken.None);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        using (BackfillRecordsJob firstRun = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance))
        {
            await firstRun.StartAsync(CancellationToken.None);
            await (firstRun.ExecuteTask ?? Task.CompletedTask);
        }

        string insertDuplicateSql =
            $"""
            INSERT INTO Records (
                EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            SELECT
                EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId,
                Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, 'duplicate'
            FROM Records
            WHERE AttemptId = {athlete.SquatAttemptId}
            AND RecordCategoryId = {(int)RecordCategory.Squat};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(
            insertDuplicateSql,
            TestContext.Current.CancellationToken);

        // Act
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);
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
            .Where(r => r.AttemptId == athlete.SquatAttemptId)
            .ToListAsync(CancellationToken.None);

        squatRecords.Count.ShouldBe(
            1,
            "duplicate records for the same attempt should be deduplicated");
    }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await ResetToBaseStateAsync();
    }

    private static async Task SeedDeadliftMeetBackfillDataAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            DELETE FROM Records
            WHERE RecordCategoryId = {(int)RecordCategory.DeadliftSingle}
            AND IsRaw = 1
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg};

            DELETE FROM Attempts WHERE AttemptId IN ({DeadliftMeetAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {DeadliftMeetParticipationId};

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({DeadliftMeetParticipationId}, {TestSeedConstants.Athlete.Id}, {Constants.DeadliftMeet.Id}, 80.5, {TestSeedConstants.WeightCategory.Id83Kg}, {TestSeedConstants.AgeCategory.OpenId}, 1, 0, 0.0, 0.0, 280.0, 0.0, 0.0, 0.0, 1);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({DeadliftMeetAttemptId}, {DeadliftMeetParticipationId}, 3, 1, 280.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedBackfillTotalTestDataAsync(ResultsDbContext dbContext)
    {
        await SeedBackfillTestDataAsync(dbContext);

        // Add bench and deadlift attempts so participation has valid total for backfill
        string sql =
            $"""
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({BackfillTestBenchAttemptId}, {BackfillTestParticipationId}, 2, 1, 140.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({BackfillTestDeadliftAttemptId}, {BackfillTestParticipationId}, 3, 1, 260.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;

            DELETE FROM Records
            WHERE EraId = {TestSeedConstants.Era.CurrentId}
            AND AgeCategoryId = {TestSeedConstants.AgeCategory.JuniorId}
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id93Kg}
            AND RecordCategoryId = {(int)RecordCategory.Total}
            AND IsRaw = 1;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedBackfillTestDataAsync(ResultsDbContext dbContext)
    {
        // Use an isolated slot: era=2, junior(2), 93kg(2), squat, raw=true.
        // No other test uses this combination.
        int eraId = TestSeedConstants.Era.CurrentId;
        int ageCategoryId = TestSeedConstants.AgeCategory.JuniorId;
        int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string cleanupSql =
            $"""
            DELETE FROM Records
            WHERE EraId = {eraId}
            AND AgeCategoryId = {ageCategoryId}
            AND WeightCategoryId = {weightCategoryId}
            AND RecordCategoryId = 1
            AND IsRaw = 1;

            DELETE FROM Attempts WHERE AttemptId IN ({BackfillTestAttemptLowId}, {BackfillTestAttemptHighId});
            DELETE FROM Participations WHERE ParticipationId = {BackfillTestParticipationId};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(cleanupSql);

        // Create a participation in junior age category, 93kg, in the raw test meet (MeetId=1)
        // Temporarily set athlete DoB to junior range so biological age resolves correctly
        string seedDataSql =
            $"""
            UPDATE Athletes SET DateOfBirth = '2003-01-01' WHERE AthleteId = {TestSeedConstants.Athlete.Id};

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({BackfillTestParticipationId}, {TestSeedConstants.Athlete.Id}, {TestSeedConstants.Meet.Id}, 90.0, {weightCategoryId}, {ageCategoryId}, 1, 0, 220.0, 140.0, 260.0, 620.0, 420.0, 90.0, 99);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({BackfillTestAttemptLowId}, {BackfillTestParticipationId}, 1, 1, 180.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({BackfillTestAttemptHighId}, {BackfillTestParticipationId}, 1, 2, 220.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedDataSql);

        // Seed corrupt records in this slot:
        // 1. Orphan seed record (no attempt) at 150kg marked as current — should be deleted
        // 2. Corrupt record at 160kg with no matching attempt, marked as current — should be deleted
        string corruptRecordsSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES ({eraId}, {ageCategoryId}, {weightCategoryId}, 1, 150.0, '2025-01-01', 0, NULL, 1, 1, 'seed');

            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES ({eraId}, {ageCategoryId}, {weightCategoryId}, 1, 160.0, '2025-02-01', 0, NULL, 1, 1, 'seed');
            """;

        await dbContext.Database.ExecuteSqlRawAsync(corruptRecordsSql);
    }

    private async Task ResetToBaseStateAsync()
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        // Delete ALL records first (FK-safe: records reference attempts)
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Records;");

        // Delete test-created attempts (hardcoded IDs from tests 1-4)
        string deleteAttemptsSql =
            $"""
            DELETE FROM Attempts WHERE AttemptId IN (
                {BackfillTestAttemptLowId}, {BackfillTestAttemptHighId},
                {BackfillTestBenchAttemptId}, {BackfillTestDeadliftAttemptId},
                {DeadliftMeetAttemptId},
                {IntermediateTotalDlRound2AttemptId}, {IntermediateTotalDlRound3AttemptId},
                {FourthAttemptRound4AttemptId});
            """;
        await dbContext.Database.ExecuteSqlRawAsync(deleteAttemptsSql);

        // Delete test-created participations (hardcoded IDs from tests 1-4)
        string deleteParticipationsSql =
            $"""
            DELETE FROM Participations WHERE ParticipationId IN (
                {BackfillTestParticipationId}, {DeadliftMeetParticipationId});
            """;
        await dbContext.Database.ExecuteSqlRawAsync(deleteParticipationsSql);

        // Delete RecordTestAthleteBuilder-created entities (tests 6-12)
        // Builder creates athlete=baseId, participation=baseId, attempts=baseId/baseId+1/baseId+2
        int[] baseIds =
        [
            NonIcelandicAthleteBaseId,
            SortOrderAthlete1BaseId, SortOrderAthlete2BaseId,
            SingleLiftAthleteBaseId,
            StandardRecordAthleteABaseId, StandardRecordAthleteBBaseId,
            IntermediateTotalAthleteBaseId,
            FourthAttemptAthleteBaseId,
            DuplicateRecordAthleteBaseId
        ];

        foreach (int baseId in baseIds)
        {
            int squatId = baseId;
            int benchId = baseId + 1;
            int deadliftId = baseId + 2;
            string deleteAttempts =
                $"DELETE FROM Attempts WHERE AttemptId IN ({squatId}, {benchId}, {deadliftId});";
            await dbContext.Database.ExecuteSqlRawAsync(deleteAttempts);
            string deleteParticipation =
                $"DELETE FROM Participations WHERE ParticipationId = {baseId};";
            await dbContext.Database.ExecuteSqlRawAsync(deleteParticipation);
            string deleteAthlete =
                $"DELETE FROM Athletes WHERE AthleteId = {baseId};";
            await dbContext.Database.ExecuteSqlRawAsync(deleteAthlete);
        }

        // Restore athlete DoB (tests 1-3 modify it)
        string restoreDobSql =
            $"""
            UPDATE Athletes SET DateOfBirth = '{SeedAthleteDateOfBirth}'
            WHERE AthleteId = {TestSeedConstants.Athlete.Id};
            """;
        await dbContext.Database.ExecuteSqlRawAsync(restoreDobSql);

        // Re-seed base records and corruption records
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedBaseRecords());
        await dbContext.Database.ExecuteSqlRawAsync(SeedRecordCorruptionSql);
    }
}