using KRAFT.Results.Tests.Shared;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed record SeedRecordAthlete(
    int AthleteId,
    int ParticipationId,
    int SquatAttemptId,
    int BenchAttemptId,
    int DeadliftAttemptId,
    int WeightCategoryId)
{
    internal async Task DeleteAsync(
        ResultsDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        string cleanupSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({SquatAttemptId}, {BenchAttemptId}, {DeadliftAttemptId});
            DELETE FROM Attempts WHERE AttemptId IN ({SquatAttemptId}, {BenchAttemptId}, {DeadliftAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {ParticipationId};
            DELETE FROM Athletes WHERE AthleteId = {AthleteId};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(cleanupSql, cancellationToken);
    }

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