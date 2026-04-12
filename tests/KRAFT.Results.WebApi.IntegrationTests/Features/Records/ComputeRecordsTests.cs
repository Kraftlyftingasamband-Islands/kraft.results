using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using RecordEntity = KRAFT.Results.WebApi.Features.Records.Record;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

public sealed class ComputeRecordsTests(IntegrationTestFixture fixture)
{
    private const int RecordTestParticipationId = 100;
    private const int RecordTestAttemptId = 100;
    private const string AttemptWeightSql = "300.0";
    private const decimal AttemptWeight = 300.0m;

    [Fact]
    public async Task WhenGoodAttemptBeatsCurrentRecord_CreatesRecordAndCascades()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedRecordComputationTestDataAsync(dbContext);

        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        // Act
        await service.ComputeRecordsAsync(RecordTestAttemptId, CancellationToken.None);

        // Assert — records should exist for full Masters4 cascade: masters4, masters3, masters2, masters1, open
        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == RecordTestAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        List<string> cascadeSlugs = createdRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");

        createdRecords.ShouldAllBe(r => r.Weight == AttemptWeight);
        createdRecords.ShouldAllBe(r => r.EraId == TestSeedConstants.Era.CurrentId);
        createdRecords.ShouldAllBe(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg);
    }

    private static async Task SeedRecordComputationTestDataAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            -- Clear any existing records for masters age categories in raw squat 83kg to avoid interference
            DELETE FROM Records
            WHERE AgeCategoryId IN ({TestSeedConstants.AgeCategory.Masters1Id}, {TestSeedConstants.AgeCategory.Masters2Id}, {TestSeedConstants.AgeCategory.Masters3Id}, {TestSeedConstants.AgeCategory.Masters4Id})
            AND RecordCategoryId = 1
            AND IsRaw = 1
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg};

            -- Also clear the open raw squat 83kg record to ensure our attempt beats it
            DELETE FROM Records
            WHERE AgeCategoryId = {TestSeedConstants.AgeCategory.OpenId}
            AND RecordCategoryId = 1
            AND IsRaw = 1
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg};

            -- Participation in Masters4 age category for athlete 1 in test meet 1
            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({RecordTestParticipationId}, {TestSeedConstants.Athlete.Id}, {TestSeedConstants.Meet.Id}, 80.5, {TestSeedConstants.WeightCategory.Id83Kg}, {TestSeedConstants.AgeCategory.Masters4Id}, 1, 0, {AttemptWeightSql}, 130.0, 250.0, 680.0, 450.0, 95.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            -- Good squat attempt that beats any existing record
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({RecordTestAttemptId}, {RecordTestParticipationId}, 1, 1, {AttemptWeightSql}, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}