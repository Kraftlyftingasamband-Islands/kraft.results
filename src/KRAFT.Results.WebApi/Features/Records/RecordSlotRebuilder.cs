using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;

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

    internal static bool HasValidTotal(Participation participation, Meet meet)
    {
        IReadOnlyList<Discipline> requiredDisciplines =
            MeetDisciplineResolver.ResolveDisciplines(
                meet.MeetType.MeetTypeId,
                meet.MeetType.Title);

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

            if (!HasValidTotal(participation, meet))
            {
                continue;
            }

            RecordCategory recordCategory =
                MeetDisciplineResolver.MapDisciplineToRecordCategory(
                    attempt.Discipline,
                    meet.MeetType.MeetTypeId,
                    meet.MeetType.Title);

            if (recordCategory == RecordCategory.None)
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

                SlotAttempt slotAttempt = new(
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
                    meet.IsRaw);

                result.Add(slotAttempt);
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

            IReadOnlyList<Discipline> requiredDisciplines =
                MeetDisciplineResolver.ResolveDisciplines(
                    meet.MeetType.MeetTypeId,
                    meet.MeetType.Title);

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

            Attempt? bestDeadlift = group
                .Where(a => a.Discipline == Discipline.Deadlift)
                .Where(a => a.Good)
                .Where(a => a.Weight > 0)
                .OrderByDescending(a => a.Weight)
                .ThenByDescending(a => a.AttemptId)
                .FirstOrDefault();

            if (bestDeadlift is null)
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

                SlotAttempt slotAttempt = new(
                    FormatSlotKey(
                        era.EraId,
                        ageCategoryId,
                        participation.WeightCategoryId,
                        RecordCategory.Total,
                        meet.IsRaw),
                    bestDeadlift.AttemptId,
                    participation.Total,
                    meetDate,
                    era.EraId,
                    ageCategoryId,
                    participation.WeightCategoryId,
                    RecordCategory.Total,
                    meet.IsRaw);

                result.Add(slotAttempt);
            }
        }

        return result;
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