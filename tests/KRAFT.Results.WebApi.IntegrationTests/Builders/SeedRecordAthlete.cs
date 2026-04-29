using KRAFT.Results.Tests.Shared;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal static class SeedRecordAthlete
{
    internal static async Task ClearSlotAsync(
        ResultsDbContext dbContext,
        int weightCategoryId,
        CancellationToken cancellationToken = default)
    {
        string clearSql =
            $"""
            DELETE FROM Records
            WHERE RecordCategoryId IN (1, 2, 3, 4, 5, 6) AND IsRaw = 1
            AND WeightCategoryId = {weightCategoryId};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(clearSql, cancellationToken);
    }
}