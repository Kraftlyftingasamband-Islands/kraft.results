using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records;

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

    private static async Task RestoreSeedRecordsAsync(ResultsDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            $"""
            DELETE FROM Records;
            DELETE FROM Attempts WHERE AttemptId IN ({BackfillTestAttemptLowId}, {BackfillTestAttemptHighId});
            DELETE FROM Participations WHERE ParticipationId = {BackfillTestParticipationId};
            """);

        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedBaseRecords());
        await dbContext.Database.ExecuteSqlRawAsync(SeedRecordCorruptionSql);
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