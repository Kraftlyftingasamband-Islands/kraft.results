using KRAFT.Results.WebApi.Enums;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal static class SeedRecordAthlete
{
    internal static async Task ClearSlotAsync(
        ResultsDbContext dbContext,
        int weightCategoryId,
        CancellationToken cancellationToken = default)
    {
        int squat = (int)RecordCategory.Squat;
        int bench = (int)RecordCategory.Bench;
        int deadlift = (int)RecordCategory.Deadlift;
        int total = (int)RecordCategory.Total;
        int benchSingle = (int)RecordCategory.BenchSingle;
        int deadliftSingle = (int)RecordCategory.DeadliftSingle;

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM Records
            WHERE RecordCategoryId IN ({squat}, {bench}, {deadlift}, {total}, {benchSingle}, {deadliftSingle})
            AND IsRaw = 1
            AND WeightCategoryId = {weightCategoryId};
            """,
            cancellationToken);
    }
}