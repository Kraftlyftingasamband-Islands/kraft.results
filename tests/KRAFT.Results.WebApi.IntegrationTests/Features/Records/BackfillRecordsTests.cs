using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using RecordEntity = KRAFT.Results.WebApi.Features.Records.Record;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

public sealed class BackfillRecordsTests(IntegrationTestFixture fixture)
{
    private const int BackfillTestParticipationId = 500;
    private const int BackfillTestAttemptLowId = 500;
    private const int BackfillTestAttemptHighId = 501;
    private const int BackfillTestBenchAttemptId = 502;
    private const int BackfillTestDeadliftAttemptId = 503;
    private const int DeadliftMeetParticipationId = 600;
    private const int DeadliftMeetAttemptId = 600;
    private const int NonIcelandicAthleteBaseId = 700;
    private const int NorwayCountryId = 2;

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
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedBackfillTestDataAsync(dbContext);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        try
        {
            // Act
            await job.StartAsync(CancellationToken.None);
            await (job.ExecuteTask ?? Task.CompletedTask);

            // Assert — slot: era=2, ageCategory=junior(2), weightCategory=93kg(2), squat(1), isRaw=true.
            // Expected chain: attempt 500 (180kg) -> attempt 501 (220kg, current).
            // The orphan seed record at 150kg (no attempt) should be deleted.
            // The corrupt record at 160kg (no matching attempt) should be deleted.
            await using AsyncServiceScope assertScope = fixture.Factory.Services.CreateAsyncScope();
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
        finally
        {
            await RestoreSeedRecordsAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenBackfillRunsTwice_ResultIsIdempotent()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        try
        {
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
            await using AsyncServiceScope assertScope = fixture.Factory.Services.CreateAsyncScope();
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
        finally
        {
            await RestoreSeedRecordsAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenBackfillRuns_TotalRecordIsCreated()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedBackfillTotalTestDataAsync(dbContext);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        try
        {
            // Act
            await job.StartAsync(CancellationToken.None);
            await (job.ExecuteTask ?? Task.CompletedTask);

            // Assert — Total record should exist for the participation with Total=620
            await using AsyncServiceScope assertScope = fixture.Factory.Services.CreateAsyncScope();
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
        finally
        {
            await RestoreSeedRecordsAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenBackfillRuns_DeadliftSingleRecordIsCreated()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedDeadliftMeetBackfillDataAsync(dbContext);

        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        try
        {
            // Act
            await job.StartAsync(CancellationToken.None);
            await (job.ExecuteTask ?? Task.CompletedTask);

            // Assert — DeadliftSingle record should exist
            await using AsyncServiceScope assertScope = fixture.Factory.Services.CreateAsyncScope();
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
        finally
        {
            await RestoreDeadliftMeetBackfillDataAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenNonIcelandicAthleteCompetes_BackfillDoesNotCreateRecord()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
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

        try
        {
            // Act
            await job.StartAsync(CancellationToken.None);
            await (job.ExecuteTask ?? Task.CompletedTask);

            // Assert — no records should exist for the non-Icelandic athlete's slot
            await using AsyncServiceScope assertScope = fixture.Factory.Services.CreateAsyncScope();
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
        finally
        {
            await norwegianAthlete.DeleteAsync(dbContext, CancellationToken.None);
            await RestoreSeedRecordsAsync(dbContext);
        }
    }

    private static async Task RestoreSeedRecordsAsync(ResultsDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            $"""
            DELETE FROM Records;
            DELETE FROM Attempts WHERE AttemptId IN ({BackfillTestAttemptLowId}, {BackfillTestAttemptHighId}, {BackfillTestBenchAttemptId}, {BackfillTestDeadliftAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {BackfillTestParticipationId};
            """);

        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedBaseRecords());
        await dbContext.Database.ExecuteSqlRawAsync(SeedRecordCorruptionSql);
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

    private static async Task RestoreDeadliftMeetBackfillDataAsync(ResultsDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            $"""
            DELETE FROM Records;
            DELETE FROM Attempts WHERE AttemptId = {DeadliftMeetAttemptId};
            DELETE FROM Participations WHERE ParticipationId = {DeadliftMeetParticipationId};
            """);

        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedBaseRecords());
        await dbContext.Database.ExecuteSqlRawAsync(SeedRecordCorruptionSql);
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
        string seedDataSql =
            $"""
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
}