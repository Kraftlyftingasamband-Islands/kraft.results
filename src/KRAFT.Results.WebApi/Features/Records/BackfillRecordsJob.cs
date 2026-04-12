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
    ILogger<BackfillRecordsJob> logger) : BackgroundService
{
    private const string CreatedBySystem = "system";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BackfillRecordsJob> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<Era> eras = await dbContext.Set<Era>()
            .AsNoTracking()
            .ToListAsync(stoppingToken);

        Dictionary<string, int> slugToIdMap = await dbContext.Set<AgeCategory>()
            .AsNoTracking()
            .Where(ac => ac.Slug != null)
            .ToDictionaryAsync(ac => ac.Slug!, ac => ac.AgeCategoryId, stoppingToken);

        List<DivisionKey> divisionKeys = await GetDistinctDivisionKeys(dbContext, eras, slugToIdMap, stoppingToken);

        int recordsCreated = 0;
        int recordsDeleted = 0;
        int divisionsProcessed = 0;

        foreach (DivisionKey division in divisionKeys)
        {
            divisionsProcessed++;
            string slotKey = FormatSlotKey(
                division.EraId,
                division.AgeCategoryId,
                division.WeightCategoryId,
                division.RecordCategory,
                division.IsRaw);

            Era? era = eras.FirstOrDefault(e => e.EraId == division.EraId);

            if (era is null)
            {
                continue;
            }

            List<Attempt> divisionAttempts = await dbContext.Set<Attempt>()
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
                .Where(a => a.Participation.Meet.IsRaw == division.IsRaw)
                .Where(a => a.Participation.WeightCategoryId == division.WeightCategoryId)
                .ToListAsync(stoppingToken);

            List<SlotAttempt> slotAttempts = BuildSlotAttempts(divisionAttempts, eras, slugToIdMap)
                .Where(sa => sa.SlotKey == slotKey)
                .OrderBy(sa => sa.MeetDate)
                .ThenBy(sa => sa.AttemptId)
                .ToList();

            List<Record> slotRecords = await dbContext.Set<Record>()
                .AsNoTracking()
                .Where(r => r.EraId == division.EraId)
                .Where(r => r.AgeCategoryId == division.AgeCategoryId)
                .Where(r => r.WeightCategoryId == division.WeightCategoryId)
                .Where(r => r.RecordCategoryId == division.RecordCategory)
                .Where(r => r.IsRaw == division.IsRaw)
                .ToListAsync(stoppingToken);

            List<ExpectedRecord> expectedChain = ComputeExpectedChain(slotAttempts);

            HashSet<int> expectedAttemptIds = expectedChain
                .Select(e => e.AttemptId)
                .ToHashSet();

            List<int> recordIdsToDelete = slotRecords
                .Where(r => r.AttemptId is null || !expectedAttemptIds.Contains(r.AttemptId.Value))
                .Select(r => r.RecordId)
                .ToList();

            if (recordIdsToDelete.Count > 0)
            {
                await dbContext.Set<Record>()
                    .Where(r => recordIdsToDelete.Contains(r.RecordId))
                    .ExecuteDeleteAsync(stoppingToken);

                recordsDeleted += recordIdsToDelete.Count;
            }

            Dictionary<int, Record> existingByAttemptId = slotRecords
                .Where(r => r.AttemptId is not null)
                .Where(r => expectedAttemptIds.Contains(r.AttemptId!.Value))
                .GroupBy(r => r.AttemptId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            List<int> recordIdsToDemote = [];
            List<int> recordIdsToSetCurrent = [];
            List<Record> recordsToCreate = [];

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

            if (recordIdsToDemote.Count > 0)
            {
                await dbContext.Set<Record>()
                    .Where(r => recordIdsToDemote.Contains(r.RecordId))
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(r => r.IsCurrent, false),
                        stoppingToken);
            }

            if (recordIdsToSetCurrent.Count > 0)
            {
                await dbContext.Set<Record>()
                    .Where(r => recordIdsToSetCurrent.Contains(r.RecordId))
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(r => r.IsCurrent, true),
                        stoppingToken);
            }

            if (recordsToCreate.Count > 0)
            {
                dbContext.Set<Record>().AddRange(recordsToCreate);
                await dbContext.SaveChangesAsync(stoppingToken);
                dbContext.ChangeTracker.Clear();
            }
        }

        _logger.LogInformation(
            "Backfill complete: {DivisionsProcessed} divisions processed, {RecordsCreated} records created, {RecordsDeleted} records deleted",
            divisionsProcessed,
            recordsCreated,
            recordsDeleted);
    }

    private static async Task<List<DivisionKey>> GetDistinctDivisionKeys(
        ResultsDbContext dbContext,
        List<Era> eras,
        Dictionary<string, int> slugToIdMap,
        CancellationToken cancellationToken)
    {
        List<DivisionKey> fromRecords = await dbContext.Set<Record>()
            .AsNoTracking()
            .Select(r => new DivisionKey(
                r.EraId,
                r.AgeCategoryId,
                r.WeightCategoryId,
                r.RecordCategoryId,
                r.IsRaw))
            .Distinct()
            .ToListAsync(cancellationToken);

        List<AttemptProjection> attemptProjections = await dbContext.Set<Attempt>()
            .AsNoTracking()
            .Where(a => a.Good)
            .Where(a => a.Weight > 0)
            .Where(a => a.Participation.Meet.RecordsPossible)
            .Select(a => new AttemptProjection(
                a.Discipline,
                a.Participation.Meet.MeetType.MeetTypeId,
                a.Participation.Meet.StartDate,
                a.Participation.AgeCategory.Slug,
                a.Participation.WeightCategoryId,
                a.Participation.Meet.IsRaw))
            .Distinct()
            .ToListAsync(cancellationToken);

        HashSet<DivisionKey> keys = [.. fromRecords];

        foreach (AttemptProjection projection in attemptProjections)
        {
            if (string.IsNullOrEmpty(projection.AgeCategorySlug))
            {
                continue;
            }

            DateOnly meetDate = DateOnly.FromDateTime(projection.MeetStartDate);
            Era? era = eras.FirstOrDefault(e => e.StartDate <= meetDate && e.EndDate >= meetDate);

            if (era is null)
            {
                continue;
            }

            RecordCategory recordCategory = MapDisciplineToRecordCategory(
                projection.Discipline,
                projection.MeetTypeId);

            if (recordCategory == RecordCategory.None)
            {
                continue;
            }

            IReadOnlyList<string> cascadeSlugs = AgeCategory.GetCascadeSlugs(projection.AgeCategorySlug);

            foreach (string cascadeSlug in cascadeSlugs)
            {
                if (!slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
                {
                    continue;
                }

                keys.Add(new DivisionKey(
                    era.EraId,
                    ageCategoryId,
                    projection.WeightCategoryId,
                    recordCategory,
                    projection.IsRaw));
            }
        }

        return [.. keys];
    }

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

    private sealed record DivisionKey(
        int EraId,
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategory,
        bool IsRaw);

    private sealed record AttemptProjection(
        Discipline Discipline,
        int MeetTypeId,
        DateTime MeetStartDate,
        string? AgeCategorySlug,
        int WeightCategoryId,
        bool IsRaw);

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