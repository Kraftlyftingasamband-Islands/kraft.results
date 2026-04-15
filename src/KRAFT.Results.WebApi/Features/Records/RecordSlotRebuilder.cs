using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordSlotRebuilder
{
    private const string CreatedBySystem = "system";

    internal static async Task<ILookup<string, Record>> BatchLoadSlotRecordsAsync(
        IReadOnlyList<SlotKey> slots,
        ResultsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        HashSet<int> eraIds = slots.Select(s => s.EraId).ToHashSet();
        HashSet<int> ageCategoryIds = slots.Select(s => s.AgeCategoryId).ToHashSet();
        HashSet<int> weightCategoryIds = slots.Select(s => s.WeightCategoryId).ToHashSet();
        HashSet<RecordCategory> recordCategories = slots.Select(s => s.RecordCategory).ToHashSet();
        HashSet<bool> isRawValues = slots.Select(s => s.IsRaw).ToHashSet();

        List<Record> allRecords = await dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => eraIds.Contains(r.EraId))
            .Where(r => ageCategoryIds.Contains(r.AgeCategoryId))
            .Where(r => weightCategoryIds.Contains(r.WeightCategoryId))
            .Where(r => recordCategories.Contains(r.RecordCategoryId))
            .Where(r => isRawValues.Contains(r.IsRaw))
            .ToListAsync(cancellationToken);

        return allRecords.ToLookup(r => FormatSlotKey(
            r.EraId,
            r.AgeCategoryId,
            r.WeightCategoryId,
            r.RecordCategoryId,
            r.IsRaw));
    }

    internal static List<ExpectedRecord> ComputeExpectedChain(
        IReadOnlyList<SlotAttempt> orderedAttempts,
        decimal startWeight = 0m)
    {
        List<ExpectedRecord> chain = [];
        decimal runningMax = startWeight;

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

    internal static SlotReconciliationResult ReconcileSlot(
        SlotKey slot,
        List<ExpectedRecord> expectedChain,
        ILookup<string, Record> allRecordsBySlot)
    {
        string slotKey = FormatSlotKey(
            slot.EraId,
            slot.AgeCategoryId,
            slot.WeightCategoryId,
            slot.RecordCategory,
            slot.IsRaw);

        List<Record> slotRecords = allRecordsBySlot[slotKey].ToList();

        return ReconcileSlotFromRecords(slotRecords, expectedChain);
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

        return ReconcileSlotFromRecords(slotRecords, expectedChain);
    }

    internal static bool HasValidTotal(Participation participation, Meet meet)
    {
        IReadOnlyList<Discipline> requiredDisciplines = meet.Category.GetDisciplines();

        foreach (Discipline discipline in requiredDisciplines)
        {
            decimal bestLift = discipline switch
            {
                Discipline.Squat => participation.Squat,
                Discipline.Bench => participation.Benchpress,
                Discipline.Deadlift => participation.Deadlift,
                _ => 0m,
            };

            if (bestLift <= 0)
            {
                return false;
            }
        }

        return true;
    }

    internal static List<SlotAttempt> BuildSlotAttempts(
        List<Attempt> attempts,
        List<Era> eras,
        Dictionary<string, int> slugToIdMap)
    {
        List<SlotAttempt> result = [];

        foreach (Attempt attempt in attempts)
        {
            Participation participation = attempt.Participation;
            Meet meet = participation.Meet;
            DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);
            Athlete athlete = participation.Athlete;

            if (!athlete.IsEligibleForRecord(meetDate))
            {
                continue;
            }

            Era? era = eras.FirstOrDefault(
                e => e.StartDate <= meetDate && e.EndDate >= meetDate);

            if (era is null)
            {
                continue;
            }

            RecordCategory recordCategory = meet.Category.MapDisciplineToRecordCategory(attempt.Discipline);

            if (recordCategory == RecordCategory.None)
            {
                continue;
            }

            // Bench/deadlift from full powerlifting meets also count towards single-lift slots
            // regardless of whether the athlete has a valid total — a bench or deadlift can set
            // a single-lift record even if the athlete bombed out on squats.
            RecordCategory? singleLiftCategory = recordCategory switch
            {
                RecordCategory.Bench => RecordCategory.BenchSingle,
                RecordCategory.Deadlift => RecordCategory.DeadliftSingle,
                _ => null,
            };

            bool validTotal = HasValidTotal(participation, meet);

            // Main lift categories (Squat/Bench/Deadlift from powerlifting meets) require a
            // valid total per IPF rules. Single-lift categories do not.
            if (!validTotal && singleLiftCategory is null)
            {
                continue;
            }

            string biologicalSlug = AgeCategory.ResolveSlug(athlete.DateOfBirth, meetDate);
            IReadOnlyList<string> cascadeSlugs = AgeCategory.GetCascadeSlugs(biologicalSlug);

            foreach (string cascadeSlug in cascadeSlugs)
            {
                if (!slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
                {
                    continue;
                }

                if (validTotal)
                {
                    result.Add(new SlotAttempt(
                        FormatSlotKey(
                            era.EraId,
                            ageCategoryId,
                            participation.WeightCategoryId,
                            recordCategory,
                            meet.IsRaw),
                        attempt.AttemptId,
                        attempt.Weight,
                        meetDate,
                        era.EraId,
                        ageCategoryId,
                        participation.WeightCategoryId,
                        recordCategory,
                        meet.IsRaw));
                }

                if (singleLiftCategory is not null)
                {
                    result.Add(new SlotAttempt(
                        FormatSlotKey(
                            era.EraId,
                            ageCategoryId,
                            participation.WeightCategoryId,
                            singleLiftCategory.Value,
                            meet.IsRaw),
                        attempt.AttemptId,
                        attempt.Weight,
                        meetDate,
                        era.EraId,
                        ageCategoryId,
                        participation.WeightCategoryId,
                        singleLiftCategory.Value,
                        meet.IsRaw));
                }
            }
        }

        return result;
    }

    internal static List<SlotAttempt> BuildTotalSlotAttempts(
        List<Attempt> attempts,
        List<Era> eras,
        Dictionary<string, int> slugToIdMap)
    {
        List<SlotAttempt> result = [];

        IEnumerable<IGrouping<int, Attempt>> byParticipation =
            attempts.GroupBy(a => a.ParticipationId);

        foreach (IGrouping<int, Attempt> group in byParticipation)
        {
            Attempt firstAttempt = group.First();
            Participation participation = firstAttempt.Participation;
            Meet meet = participation.Meet;

            IReadOnlyList<Discipline> requiredDisciplines = meet.Category.GetDisciplines();

            if (requiredDisciplines.Count <= 1)
            {
                continue;
            }

            if (participation.Total <= 0)
            {
                continue;
            }

            DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);
            Athlete athlete = participation.Athlete;

            if (!athlete.IsEligibleForRecord(meetDate))
            {
                continue;
            }

            Era? era = eras.FirstOrDefault(
                e => e.StartDate <= meetDate && e.EndDate >= meetDate);

            if (era is null)
            {
                continue;
            }

            if (!HasValidTotal(participation, meet))
            {
                continue;
            }

            // 4th attempts don't count towards the competition total — use the
            // participation's stored bests which reflect rounds 1-3 only.
            decimal bestSquat = participation.Squat;
            decimal bestBench = participation.Benchpress;

            List<Attempt> goodDeadlifts = group
                .Where(a => a.Discipline == Discipline.Deadlift)
                .Where(a => a.Good)
                .Where(a => a.Weight > 0)
                .Where(a => a.Round <= 3)
                .OrderBy(a => a.Weight)
                .ThenBy(a => a.AttemptId)
                .ToList();

            if (goodDeadlifts.Count == 0)
            {
                continue;
            }

            string biologicalSlug = AgeCategory.ResolveSlug(athlete.DateOfBirth, meetDate);
            IReadOnlyList<string> cascadeSlugs = AgeCategory.GetCascadeSlugs(biologicalSlug);

            foreach (Attempt deadliftAttempt in goodDeadlifts)
            {
                decimal runningTotal = bestSquat + bestBench + deadliftAttempt.Weight;

                foreach (string cascadeSlug in cascadeSlugs)
                {
                    if (!slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
                    {
                        continue;
                    }

                    SlotAttempt slotAttempt = new(
                        FormatSlotKey(
                            era.EraId,
                            ageCategoryId,
                            participation.WeightCategoryId,
                            RecordCategory.Total,
                            meet.IsRaw),
                        deadliftAttempt.AttemptId,
                        runningTotal,
                        meetDate,
                        era.EraId,
                        ageCategoryId,
                        participation.WeightCategoryId,
                        RecordCategory.Total,
                        meet.IsRaw);

                    result.Add(slotAttempt);
                }
            }
        }

        return result;
    }

    internal static List<SlotAttempt> FilterAndOrderForChain(
        IEnumerable<SlotAttempt> slotAttempts,
        string slotKey)
    {
        return slotAttempts
            .Where(sa => sa.SlotKey == slotKey)
            .OrderBy(sa => sa.MeetDate)
            .ThenBy(sa => sa.Weight)
            .ThenBy(sa => sa.AttemptId)
            .ToList();
    }

    internal static string FormatSlotKey(
        int eraId,
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory recordCategory,
        bool isRaw)
    {
        return $"{eraId}-{ageCategoryId}-{weightCategoryId}-{(int)recordCategory}-{isRaw}";
    }

    internal static StandardRecordInfo? GetStandardRecord(
        SlotKey slot,
        ILookup<string, Record> allRecordsBySlot)
    {
        string slotKey = FormatSlotKey(
            slot.EraId,
            slot.AgeCategoryId,
            slot.WeightCategoryId,
            slot.RecordCategory,
            slot.IsRaw);

        Record? standard = allRecordsBySlot[slotKey]
            .Where(r => r.IsStandard)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault();

        if (standard is null)
        {
            return null;
        }

        return new StandardRecordInfo(standard.Weight, standard.Date);
    }

    internal static async Task<StandardRecordInfo?> GetLatestStandardRecordAsync(
        int eraId,
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory recordCategory,
        bool isRaw,
        ResultsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        Record? standard = await dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => r.EraId == eraId)
            .Where(r => r.AgeCategoryId == ageCategoryId)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == recordCategory)
            .Where(r => r.IsRaw == isRaw)
            .Where(r => r.IsStandard)
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (standard is null)
        {
            return null;
        }

        return new StandardRecordInfo(standard.Weight, standard.Date);
    }

    private static SlotReconciliationResult ReconcileSlotFromRecords(
        List<Record> slotRecords,
        List<ExpectedRecord> expectedChain)
    {
        HashSet<int> expectedAttemptIds = expectedChain
            .Select(e => e.AttemptId)
            .ToHashSet();

        List<int> recordIdsToDelete = slotRecords
            .Where(r => !r.IsStandard)
            .Where(r => r.AttemptId is null || !expectedAttemptIds.Contains(r.AttemptId.Value))
            .Select(r => r.RecordId)
            .ToList();

        ILookup<int, Record> existingByAttemptLookup = slotRecords
            .Where(r => r.AttemptId is not null)
            .Where(r => expectedAttemptIds.Contains(r.AttemptId!.Value))
            .ToLookup(r => r.AttemptId!.Value);

        Dictionary<int, Record> existingByAttemptId = existingByAttemptLookup
            .ToDictionary(g => g.Key, g => g.First());

        List<int> duplicateRecordIds = existingByAttemptLookup
            .SelectMany(g => g.Skip(1))
            .Select(r => r.RecordId)
            .ToList();

        recordIdsToDelete.AddRange(duplicateRecordIds);

        List<int> recordIdsToDemote = [];
        List<int> recordIdsToSetCurrent = [];
        List<(int RecordId, decimal Weight)> weightUpdates = [];
        List<Record> recordsToCreate = [];

        for (int i = 0; i < expectedChain.Count; i++)
        {
            ExpectedRecord expected = expectedChain[i];
            bool shouldBeCurrent = i == expectedChain.Count - 1;

            if (existingByAttemptId.TryGetValue(expected.AttemptId, out Record? existing))
            {
                if (existing.Weight != expected.Weight)
                {
                    weightUpdates.Add((existing.RecordId, expected.Weight));
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
            weightUpdates,
            recordsToCreate);
    }

    internal sealed record StandardRecordInfo(decimal Weight, DateOnly Date);

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
        List<(int RecordId, decimal Weight)> WeightUpdates,
        List<Record> RecordsToCreate);
}