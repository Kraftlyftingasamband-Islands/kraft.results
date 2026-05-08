using System.Data;

using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using static KRAFT.Results.WebApi.Features.Records.RecordSlotRebuilder;

namespace KRAFT.Results.WebApi.Features.Records;

internal sealed class BackfillRecordsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BackfillRecordsJob> logger) : BackgroundService
{
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

        List<DivisionKey> divisionKeys = await GetDistinctDivisionKeys(
            dbContext,
            eras,
            slugToIdMap,
            stoppingToken);

        int recordsCreated = 0;
        int recordsDeleted = 0;
        int divisionsProcessed = 0;

        IEnumerable<IGrouping<(int WeightCategoryId, bool IsRaw), DivisionKey>> groups =
            divisionKeys.GroupBy(d => (d.WeightCategoryId, d.IsRaw));

        foreach (IGrouping<(int WeightCategoryId, bool IsRaw), DivisionKey> group in groups)
        {
            List<Attempt> allAttempts = await dbContext.Set<Attempt>()
                .AsNoTracking()
                .Include(a => a.Participation)
                    .ThenInclude(p => p.Meet)
                .Include(a => a.Participation)
                    .ThenInclude(p => p.Athlete)
                        .ThenInclude(a => a.Bans)
                .Include(a => a.Participation)
                    .ThenInclude(p => p.AgeCategory)
                .Where(a => a.Good)
                .Where(a => a.Weight > 0)
                .Where(a => a.Participation.Athlete.Country.Value == RecordConstants.IcelandIso3)
                .Where(a => a.Participation.Meet.RecordsPossible)
                .Where(a => a.Participation.Meet.IsRaw == group.Key.IsRaw)
                .Where(a => a.Participation.WeightCategoryId == group.Key.WeightCategoryId)
                .ToListAsync(stoppingToken);

            List<SlotAttempt> disciplineSlotAttempts =
                BuildSlotAttempts(allAttempts, eras, slugToIdMap);
            List<SlotAttempt> totalSlotAttempts =
                BuildTotalSlotAttempts(allAttempts, eras, slugToIdMap);

            IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
            int groupDivisionsProcessed = 0;
            int groupRecordsCreated = 0;
            int groupRecordsDeleted = 0;

            await strategy.ExecuteAsync(async () =>
            {
                // Reset local counters so retries don't double-count.
                groupDivisionsProcessed = 0;
                groupRecordsCreated = 0;
                groupRecordsDeleted = 0;

                await using IDbContextTransaction transaction =
                    await dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.RepeatableRead,
                        stoppingToken);

                foreach (DivisionKey division in group)
                {
                    Era? era = eras.FirstOrDefault(e => e.EraId == division.EraId);

                    if (era is null)
                    {
                        continue;
                    }

                    groupDivisionsProcessed++;

                    string slotKey = FormatSlotKey(
                        division.EraId,
                        division.AgeCategoryId,
                        division.WeightCategoryId,
                        division.RecordCategory,
                        division.IsRaw);

                    List<SlotAttempt> slotAttempts = division.RecordCategory == RecordCategory.Total
                        ? FilterAndOrderForChain(totalSlotAttempts, slotKey)
                        : FilterAndOrderForChain(disciplineSlotAttempts, slotKey);

                    StandardRecordInfo? standardRecord = await GetLatestStandardRecordAsync(
                        division.EraId,
                        division.AgeCategoryId,
                        division.WeightCategoryId,
                        division.RecordCategory,
                        division.IsRaw,
                        dbContext,
                        stoppingToken);

                    if (standardRecord is not null)
                    {
                        slotAttempts = slotAttempts
                            .Where(sa => sa.MeetDate >= standardRecord.Date)
                            .ToList();
                    }

                    List<ExpectedRecord> expectedChain = ComputeExpectedChain(
                        slotAttempts,
                        standardRecord?.Weight ?? 0m);

                    SlotReconciliationResult result = await ReconcileSlotAsync(
                        division.EraId,
                        division.AgeCategoryId,
                        division.WeightCategoryId,
                        division.RecordCategory,
                        division.IsRaw,
                        expectedChain,
                        dbContext,
                        stoppingToken);

                    if (result.RecordIdsToDelete.Count > 0)
                    {
                        await dbContext.Set<Record>()
                            .Where(r => result.RecordIdsToDelete.Contains(r.RecordId))
                            .ExecuteDeleteAsync(stoppingToken);

                        groupRecordsDeleted += result.RecordIdsToDelete.Count;
                    }

                    if (result.RecordIdsToDemote.Count > 0)
                    {
                        await dbContext.Set<Record>()
                            .Where(r => result.RecordIdsToDemote.Contains(r.RecordId))
                            .ExecuteUpdateAsync(
                                s => s.SetProperty(r => r.IsCurrent, false),
                                stoppingToken);
                    }

                    if (result.RecordIdsToSetCurrent.Count > 0)
                    {
                        await dbContext.Set<Record>()
                            .Where(r => result.RecordIdsToSetCurrent.Contains(r.RecordId))
                            .ExecuteUpdateAsync(
                                s => s.SetProperty(r => r.IsCurrent, true),
                                stoppingToken);
                    }

                    foreach ((int recordId, decimal weight) in result.WeightUpdates)
                    {
                        await dbContext.Set<Record>()
                            .Where(r => r.RecordId == recordId)
                            .ExecuteUpdateAsync(
                                s => s.SetProperty(r => r.Weight, weight),
                                stoppingToken);
                    }

                    if (result.RecordsToCreate.Count > 0)
                    {
                        dbContext.Set<Record>().AddRange(result.RecordsToCreate);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        dbContext.ChangeTracker.Clear();

                        groupRecordsCreated += result.RecordsToCreate.Count;
                    }
                }

                await transaction.CommitAsync(stoppingToken);
            });

            divisionsProcessed += groupDivisionsProcessed;
            recordsCreated += groupRecordsCreated;
            recordsDeleted += groupRecordsDeleted;
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
            .Where(a => a.Participation.Athlete.Country.Value == RecordConstants.IcelandIso3)
            .Where(a => a.Participation.Meet.RecordsPossible)
            .Select(a => new AttemptProjection(
                a.Discipline,
                a.Participation.Meet.Category,
                a.Participation.Meet.StartDate,
                a.Participation.Athlete.DateOfBirth,
                a.Participation.WeightCategoryId,
                a.Participation.Meet.IsRaw))
            .Distinct()
            .ToListAsync(cancellationToken);

        HashSet<DivisionKey> keys = [.. fromRecords];

        foreach (AttemptProjection projection in attemptProjections)
        {
            DateOnly meetDate = DateOnly.FromDateTime(projection.MeetStartDate);
            Era? era = eras.FirstOrDefault(e => e.StartDate <= meetDate && e.EndDate >= meetDate);

            if (era is null)
            {
                continue;
            }

            RecordCategory recordCategory = projection.Category.MapDisciplineToRecordCategory(projection.Discipline);

            if (recordCategory == RecordCategory.None)
            {
                continue;
            }

            // Bench/deadlift from full powerlifting meets also contribute to single-lift slots.
            RecordCategory? singleLiftCategory = recordCategory switch
            {
                RecordCategory.Bench => RecordCategory.BenchSingle,
                RecordCategory.Deadlift => RecordCategory.DeadliftSingle,
                _ => null,
            };

            string biologicalSlug = AgeCategory.ResolveSlug(projection.AthleteDoB, meetDate);
            IReadOnlyList<string> cascadeSlugs =
                AgeCategory.GetCascadeSlugs(biologicalSlug);

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

                if (singleLiftCategory is not null)
                {
                    keys.Add(new DivisionKey(
                        era.EraId,
                        ageCategoryId,
                        projection.WeightCategoryId,
                        singleLiftCategory.Value,
                        projection.IsRaw));
                }
            }
        }

        List<TotalProjection> totalProjections = await dbContext.Set<Participation>()
            .AsNoTracking()
            .Where(p => p.Athlete.Country.Value == RecordConstants.IcelandIso3)
            .Where(p => p.Meet.RecordsPossible)
            .Where(p => p.Total > 0)
            .Select(p => new TotalProjection(
                p.Meet.Category,
                p.Meet.StartDate,
                p.Athlete.DateOfBirth,
                p.WeightCategoryId,
                p.Meet.IsRaw))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (TotalProjection projection in totalProjections)
        {
            IReadOnlyList<Discipline> requiredDisciplines = projection.Category.GetDisciplines();

            if (requiredDisciplines.Count <= 1)
            {
                continue;
            }

            DateOnly meetDate = DateOnly.FromDateTime(projection.MeetStartDate);
            Era? era = eras.FirstOrDefault(
                e => e.StartDate <= meetDate && e.EndDate >= meetDate);

            if (era is null)
            {
                continue;
            }

            string biologicalSlug = AgeCategory.ResolveSlug(projection.AthleteDoB, meetDate);
            IReadOnlyList<string> cascadeSlugs =
                AgeCategory.GetCascadeSlugs(biologicalSlug);

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
                    RecordCategory.Total,
                    projection.IsRaw));
            }
        }

        return [.. keys];
    }

    private sealed record DivisionKey(
        int EraId,
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategory,
        bool IsRaw);

    private sealed record AttemptProjection(
        Discipline Discipline,
        MeetCategory Category,
        DateTime MeetStartDate,
        DateOnly? AthleteDoB,
        int WeightCategoryId,
        bool IsRaw);

    private sealed record TotalProjection(
        MeetCategory Category,
        DateTime MeetStartDate,
        DateOnly? AthleteDoB,
        int WeightCategoryId,
        bool IsRaw);
}