using KRAFT.Results.WebApi.Enums;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordSlotRebuilder
{
    private const string CreatedBySystem = "system";

    internal static List<ExpectedRecord> ComputeExpectedChain(
        IReadOnlyList<SlotAttempt> orderedAttempts)
    {
        List<ExpectedRecord> chain = [];
        decimal runningMax = 0m;

        foreach (SlotAttempt attempt in orderedAttempts)
        {
            if (attempt.Weight > runningMax)
            {
                runningMax = attempt.Weight;
                chain.Add(new ExpectedRecord(
                    attempt.AttemptId,
                    attempt.EraId,
                    attempt.AgeCategoryId,
                    attempt.WeightCategoryId,
                    attempt.RecordCategory,
                    attempt.Weight,
                    attempt.MeetDate,
                    attempt.IsRaw));
            }
        }

        return chain;
    }

    internal static async Task<SlotReconciliationResult> ReconcileSlotAsync(
        int eraId,
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory recordCategory,
        bool isRaw,
        List<ExpectedRecord> expectedChain,
        ResultsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        List<Record> slotRecords = await dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => r.EraId == eraId)
            .Where(r => r.AgeCategoryId == ageCategoryId)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == recordCategory)
            .Where(r => r.IsRaw == isRaw)
            .ToListAsync(cancellationToken);

        HashSet<int> expectedAttemptIds = expectedChain
            .Select(e => e.AttemptId)
            .ToHashSet();

        List<int> recordIdsToDelete = slotRecords
            .Where(r => r.AttemptId is null || !expectedAttemptIds.Contains(r.AttemptId.Value))
            .Select(r => r.RecordId)
            .ToList();

        Dictionary<int, Record> existingByAttemptId = slotRecords
            .Where(r => r.AttemptId is not null)
            .Where(r => expectedAttemptIds.Contains(r.AttemptId!.Value))
            .GroupBy(r => r.AttemptId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        List<int> recordIdsToDemote = [];
        List<int> recordIdsToSetCurrent = [];
        List<int> recordIdsToUpdateWeight = [];
        List<decimal> updatedWeights = [];
        List<Record> recordsToCreate = [];

        for (int i = 0; i < expectedChain.Count; i++)
        {
            ExpectedRecord expected = expectedChain[i];
            bool shouldBeCurrent = i == expectedChain.Count - 1;

            if (existingByAttemptId.TryGetValue(expected.AttemptId, out Record? existing))
            {
                if (existing.Weight != expected.Weight)
                {
                    recordIdsToUpdateWeight.Add(existing.RecordId);
                    updatedWeights.Add(expected.Weight);
                }

                if (shouldBeCurrent && !existing.IsCurrent)
                {
                    recordIdsToSetCurrent.Add(existing.RecordId);
                }
                else if (!shouldBeCurrent && existing.IsCurrent)
                {
                    recordIdsToDemote.Add(existing.RecordId);
                }
            }
            else
            {
                Record newRecord = Record.Create(
                    expected.EraId,
                    expected.AgeCategoryId,
                    expected.WeightCategoryId,
                    expected.RecordCategory,
                    expected.Weight,
                    expected.Date,
                    expected.AttemptId,
                    expected.IsRaw,
                    CreatedBySystem);

                if (shouldBeCurrent)
                {
                    newRecord.SetCurrent();
                }

                recordsToCreate.Add(newRecord);
            }
        }

        return new SlotReconciliationResult(
            recordIdsToDelete,
            recordIdsToDemote,
            recordIdsToSetCurrent,
            recordIdsToUpdateWeight,
            updatedWeights,
            recordsToCreate);
    }

    internal sealed record SlotAttempt(
        string SlotKey,
        int AttemptId,
        decimal Weight,
        DateOnly MeetDate,
        int EraId,
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategory,
        bool IsRaw);

    internal sealed record ExpectedRecord(
        int AttemptId,
        int EraId,
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategory,
        decimal Weight,
        DateOnly Date,
        bool IsRaw);

    internal sealed record SlotReconciliationResult(
        List<int> RecordIdsToDelete,
        List<int> RecordIdsToDemote,
        List<int> RecordIdsToSetCurrent,
        List<int> RecordIdsToUpdateWeight,
        List<decimal> UpdatedWeights,
        List<Record> RecordsToCreate);
}