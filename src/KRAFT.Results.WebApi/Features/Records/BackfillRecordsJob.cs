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

internal sealed class BackfillRecordsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BackfillRecordsJob> logger) : IHostedService
{
    private const string CreatedBySystem = "system";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BackfillRecordsJob> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<Era> eras = await dbContext.Set<Era>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<string, int> slugToIdMap = await dbContext.Set<AgeCategory>()
            .AsNoTracking()
            .Where(ac => ac.Slug != null)
            .ToDictionaryAsync(ac => ac.Slug!, ac => ac.AgeCategoryId, cancellationToken);

        List<Attempt> allAttempts = await dbContext.Set<Attempt>()
            .AsNoTracking()
            .Include(a => a.Participation)
                .ThenInclude(p => p.Meet)
                    .ThenInclude(m => m.MeetType)
            .Include(a => a.Participation)
                .ThenInclude(p => p.Athlete)
                    .ThenInclude(a => a.Bans)
            .Include(a => a.Participation)
                .ThenInclude(p => p.AgeCategory)
            .Where(a => a.Good)
            .Where(a => a.Weight > 0)
            .Where(a => a.Participation.Meet.RecordsPossible)
            .ToListAsync(cancellationToken);

        List<SlotAttempt> slotAttempts = BuildSlotAttempts(allAttempts, eras, slugToIdMap);

        Dictionary<string, List<SlotAttempt>> grouped = slotAttempts
            .GroupBy(sa => sa.SlotKey)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(sa => sa.MeetDate)
                    .ThenBy(sa => sa.AttemptId)
                    .ToList());

        List<Record> existingRecords = await dbContext.Set<Record>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<string, List<Record>> existingBySlot = existingRecords
            .GroupBy(r => FormatSlotKey(r.EraId, r.AgeCategoryId, r.WeightCategoryId, r.RecordCategoryId, r.IsRaw))
            .ToDictionary(g => g.Key, g => g.ToList());

        int recordsCreated = 0;
        int recordsDeleted = 0;
        int divisionsProcessed = 0;

        HashSet<string> allSlotKeys = [.. grouped.Keys, .. existingBySlot.Keys];
        List<int> recordIdsToDelete = [];
        List<int> recordIdsToDemote = [];
        List<int> recordIdsToSetCurrent = [];
        List<Record> recordsToCreate = [];

        foreach (string slotKey in allSlotKeys)
        {
            divisionsProcessed++;

            List<SlotAttempt> slotChain = grouped.GetValueOrDefault(slotKey) ?? [];
            List<Record> slotRecords = existingBySlot.GetValueOrDefault(slotKey) ?? [];

            List<ExpectedRecord> expectedChain = ComputeExpectedChain(slotChain);

            HashSet<int> expectedAttemptIds = expectedChain
                .Select(e => e.AttemptId)
                .ToHashSet();

            // Identify records to delete (not in expected chain)
            List<Record> toDelete = slotRecords
                .Where(r => r.AttemptId is null || !expectedAttemptIds.Contains(r.AttemptId.Value))
                .ToList();

            foreach (Record record in toDelete)
            {
                recordIdsToDelete.Add(record.RecordId);
                recordsDeleted++;
            }

            Dictionary<int, Record> existingByAttemptId = slotRecords
                .Where(r => r.AttemptId is not null)
                .Where(r => expectedAttemptIds.Contains(r.AttemptId!.Value))
                .GroupBy(r => r.AttemptId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            for (int i = 0; i < expectedChain.Count; i++)
            {
                ExpectedRecord expected = expectedChain[i];
                bool shouldBeCurrent = i == expectedChain.Count - 1;

                if (existingByAttemptId.TryGetValue(expected.AttemptId, out Record? existing))
                {
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
                    recordsCreated++;
                }
            }
        }

        // Apply changes using raw SQL for deletes/updates to avoid change tracking conflicts
        if (recordIdsToDelete.Count > 0)
        {
            string deleteSql = $"DELETE FROM Records WHERE RecordId IN ({string.Join(",", recordIdsToDelete)})";
            await dbContext.Database.ExecuteSqlRawAsync(deleteSql, cancellationToken);
        }

        if (recordIdsToDemote.Count > 0)
        {
            string demoteSql = $"UPDATE Records SET IsCurrent = 0 WHERE RecordId IN ({string.Join(",", recordIdsToDemote)})";
            await dbContext.Database.ExecuteSqlRawAsync(demoteSql, cancellationToken);
        }

        if (recordIdsToSetCurrent.Count > 0)
        {
            string setCurrentSql = $"UPDATE Records SET IsCurrent = 1 WHERE RecordId IN ({string.Join(",", recordIdsToSetCurrent)})";
            await dbContext.Database.ExecuteSqlRawAsync(setCurrentSql, cancellationToken);
        }

        if (recordsToCreate.Count > 0)
        {
            dbContext.Set<Record>().AddRange(recordsToCreate);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Backfill complete: {DivisionsProcessed} divisions processed, {RecordsCreated} records created, {RecordsDeleted} records deleted",
            divisionsProcessed,
            recordsCreated,
            recordsDeleted);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static List<SlotAttempt> BuildSlotAttempts(
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

            Era? era = eras.FirstOrDefault(e => e.StartDate <= meetDate && e.EndDate >= meetDate);

            if (era is null)
            {
                continue;
            }

            if (!HasValidTotal(participation, meet))
            {
                continue;
            }

            RecordCategory recordCategory = MapDisciplineToRecordCategory(
                attempt.Discipline,
                meet.MeetType.MeetTypeId);

            if (recordCategory == RecordCategory.None)
            {
                continue;
            }

            AgeCategory ageCategory = participation.AgeCategory;
            string? slug = ageCategory.Slug;

            if (string.IsNullOrEmpty(slug))
            {
                continue;
            }

            IReadOnlyList<string> cascadeSlugs = AgeCategory.GetCascadeSlugs(slug);

            foreach (string cascadeSlug in cascadeSlugs)
            {
                if (!slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
                {
                    continue;
                }

                SlotAttempt slotAttempt = new(
                    FormatSlotKey(era.EraId, ageCategoryId, participation.WeightCategoryId, recordCategory, meet.IsRaw),
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

    private static List<ExpectedRecord> ComputeExpectedChain(List<SlotAttempt> orderedAttempts)
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

    private static bool HasValidTotal(Participation participation, Meet meet)
    {
        IReadOnlyList<Discipline> requiredDisciplines = MeetDisciplineResolver.ResolveDisciplines(
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

    private static RecordCategory MapDisciplineToRecordCategory(Discipline discipline, int meetTypeId)
    {
        bool isSingleLiftMeet = MeetDisciplineResolver.IsBenchMeetType(meetTypeId);

        return discipline switch
        {
            Discipline.Squat => RecordCategory.Squat,
            Discipline.Bench => isSingleLiftMeet ? RecordCategory.BenchSingle : RecordCategory.Bench,
            Discipline.Deadlift => RecordCategory.Deadlift,
            _ => RecordCategory.None,
        };
    }

    private static string FormatSlotKey(
        int eraId,
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory recordCategory,
        bool isRaw)
    {
        return $"{eraId}-{ageCategoryId}-{weightCategoryId}-{(int)recordCategory}-{isRaw}";
    }

    private sealed record SlotAttempt(
        string SlotKey,
        int AttemptId,
        decimal Weight,
        DateOnly MeetDate,
        int EraId,
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategory,
        bool IsRaw);

    private sealed record ExpectedRecord(
        int AttemptId,
        int EraId,
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategory,
        decimal Weight,
        DateOnly Date,
        bool IsRaw);
}