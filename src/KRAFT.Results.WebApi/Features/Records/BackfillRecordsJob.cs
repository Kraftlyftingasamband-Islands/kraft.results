using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;

using static KRAFT.Results.WebApi.Features.Records.RecordSlotRebuilder;

namespace KRAFT.Results.WebApi.Features.Records;

internal sealed class BackfillRecordsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BackfillRecordsJob> logger) : BackgroundService
{
    private const string IcelandIso3 = "ISL";

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
                .Where(a => a.Participation.Athlete.Country.Iso3 == IcelandIso3)
                .Where(a => a.Participation.Meet.RecordsPossible)
                .Where(a => a.Participation.Meet.IsRaw == division.IsRaw)
                .Where(a => a.Participation.WeightCategoryId == division.WeightCategoryId)
                .ToListAsync(stoppingToken);

            List<SlotAttempt> slotAttempts = division.RecordCategory == RecordCategory.Total
                ? BuildTotalSlotAttempts(divisionAttempts, eras, slugToIdMap)
                    .Where(sa => sa.SlotKey == slotKey)
                    .OrderBy(sa => sa.MeetDate)
                    .ThenBy(sa => sa.AttemptId)
                    .ToList()
                : BuildSlotAttempts(divisionAttempts, eras, slugToIdMap)
                    .Where(sa => sa.SlotKey == slotKey)
                    .OrderBy(sa => sa.MeetDate)
                    .ThenBy(sa => sa.AttemptId)
                    .ToList();

            List<ExpectedRecord> expectedChain = ComputeExpectedChain(slotAttempts);

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

                recordsDeleted += result.RecordIdsToDelete.Count;
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

            for (int i = 0; i < result.RecordIdsToUpdateWeight.Count; i++)
            {
                int recordId = result.RecordIdsToUpdateWeight[i];
                decimal weight = result.UpdatedWeights[i];

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

                recordsCreated += result.RecordsToCreate.Count;
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
            .Where(a => a.Participation.Athlete.Country.Iso3 == IcelandIso3)
            .Where(a => a.Participation.Meet.RecordsPossible)
            .Select(a => new AttemptProjection(
                a.Discipline,
                a.Participation.Meet.MeetType.MeetTypeId,
                a.Participation.Meet.MeetType.Title,
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

            RecordCategory recordCategory = MeetDisciplineResolver.MapDisciplineToRecordCategory(
                projection.Discipline,
                projection.MeetTypeId,
                projection.MeetTypeTitle);

            if (recordCategory == RecordCategory.None)
            {
                continue;
            }

            IReadOnlyList<string> cascadeSlugs =
                AgeCategory.GetCascadeSlugs(projection.AgeCategorySlug);

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

        List<TotalProjection> totalProjections = await dbContext.Set<Participation>()
            .AsNoTracking()
            .Where(p => p.Athlete.Country.Iso3 == IcelandIso3)
            .Where(p => p.Meet.RecordsPossible)
            .Where(p => p.Total > 0)
            .Select(p => new TotalProjection(
                p.Meet.MeetType.MeetTypeId,
                p.Meet.MeetType.Title,
                p.Meet.StartDate,
                p.AgeCategory.Slug,
                p.WeightCategoryId,
                p.Meet.IsRaw))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (TotalProjection projection in totalProjections)
        {
            if (string.IsNullOrEmpty(projection.AgeCategorySlug))
            {
                continue;
            }

            IReadOnlyList<Discipline> requiredDisciplines =
                MeetDisciplineResolver.ResolveDisciplines(
                    projection.MeetTypeId,
                    projection.MeetTypeTitle);

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

            IReadOnlyList<string> cascadeSlugs =
                AgeCategory.GetCascadeSlugs(projection.AgeCategorySlug);

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
        int MeetTypeId,
        string MeetTypeTitle,
        DateTime MeetStartDate,
        string? AgeCategorySlug,
        int WeightCategoryId,
        bool IsRaw);

    private sealed record TotalProjection(
        int MeetTypeId,
        string MeetTypeTitle,
        DateTime MeetStartDate,
        string? AgeCategorySlug,
        int WeightCategoryId,
        bool IsRaw);
}