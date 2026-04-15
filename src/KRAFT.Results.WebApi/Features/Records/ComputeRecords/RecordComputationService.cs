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

namespace KRAFT.Results.WebApi.Features.Records.ComputeRecords;

internal sealed class RecordComputationService(
    ResultsDbContext dbContext,
    ILogger<RecordComputationService> logger)
{
    private readonly ResultsDbContext _dbContext = dbContext;
    private readonly ILogger<RecordComputationService> _logger = logger;

    internal async Task ComputeRecordsAsync(
        int attemptId,
        CancellationToken cancellationToken)
    {
        Attempt? attempt = await _dbContext.Set<Attempt>()
            .Include(a => a.Participation)
                .ThenInclude(p => p.Attempts)
            .Include(a => a.Participation)
                .ThenInclude(p => p.Meet)
                    .ThenInclude(m => m.MeetType)
            .Include(a => a.Participation)
                .ThenInclude(p => p.Athlete)
                    .ThenInclude(a => a.Bans)
            .Include(a => a.Participation)
                .ThenInclude(p => p.Athlete)
                    .ThenInclude(a => a.Country)
            .Include(a => a.Participation)
                .ThenInclude(p => p.AgeCategory)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId, cancellationToken);

        if (attempt is null)
        {
            _logger.LogWarning(
                "Skipping record computation: attempt {AttemptId} not found",
                attemptId);
            return;
        }

        Participation participation = attempt.Participation;
        Meet meet = participation.Meet;

        if (!meet.RecordsPossible)
        {
            _logger.LogWarning(
                "Skipping record computation: meet {MeetSlug} has RecordsPossible=false (AttemptId: {AttemptId})",
                meet.Slug,
                attemptId);
            return;
        }

        DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);
        Athlete athlete = participation.Athlete;

        if (!athlete.IsEligibleForRecord(meetDate))
        {
            _logger.LogWarning(
                "Skipping record computation: athlete {AthleteId} is banned on {MeetDate} (AttemptId: {AttemptId})",
                athlete.AthleteId,
                meetDate,
                attemptId);
            return;
        }

        if (athlete.Country?.Iso3 != RecordConstants.IcelandIso3)
        {
            _logger.LogWarning(
                "Skipping record computation: athlete {AthleteId} is not Icelandic (AttemptId: {AttemptId})",
                athlete.AthleteId,
                attemptId);
            return;
        }

        List<Era> eras = await _dbContext.Set<Era>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Era? era = eras.FirstOrDefault(
            e => e.StartDate <= meetDate && e.EndDate >= meetDate);

        if (era is null)
        {
            _logger.LogWarning(
                "Skipping record computation: no era found for date {MeetDate} (AttemptId: {AttemptId})",
                meetDate,
                attemptId);
            return;
        }

        Dictionary<string, int> slugToIdMap = await _dbContext.Set<AgeCategory>()
            .Where(ac => ac.Slug != null)
            .ToDictionaryAsync(
                ac => ac.Slug!,
                ac => ac.AgeCategoryId,
                cancellationToken);

        if (!HasValidTotal(participation, meet))
        {
            await HandleInvalidTotalAsync(
                participation,
                slugToIdMap,
                eras,
                cancellationToken);
            return;
        }

        List<SlotKey> affectedSlots = DetermineAffectedSlots(
            participation,
            meet,
            era,
            slugToIdMap);

        if (affectedSlots.Count == 0)
        {
            return;
        }

        await FullRebuildSlotsAsync(
            affectedSlots,
            eras,
            slugToIdMap,
            cancellationToken);
    }

    internal async Task RebuildSlotsAsync(
        IReadOnlyList<SlotKey> affectedSlots,
        CancellationToken cancellationToken)
    {
        List<Era> eras = await _dbContext.Set<Era>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<string, int> slugToIdMap = await _dbContext.Set<AgeCategory>()
            .Where(ac => ac.Slug != null)
            .ToDictionaryAsync(
                ac => ac.Slug!,
                ac => ac.AgeCategoryId,
                cancellationToken);

        await FullRebuildSlotsAsync(
            affectedSlots,
            eras,
            slugToIdMap,
            cancellationToken);
    }

    internal async Task RebuildSlotsWithinTransactionAsync(
        IReadOnlyList<SlotKey> affectedSlots,
        CancellationToken cancellationToken)
    {
        List<Era> eras = await _dbContext.Set<Era>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<string, int> slugToIdMap = await _dbContext.Set<AgeCategory>()
            .Where(ac => ac.Slug != null)
            .ToDictionaryAsync(
                ac => ac.Slug!,
                ac => ac.AgeCategoryId,
                cancellationToken);

        await RebuildSlotsContentAsync(
            affectedSlots,
            eras,
            slugToIdMap,
            cancellationToken);
    }

    private static List<SlotKey> DetermineAffectedSlots(
        Participation participation,
        Meet meet,
        Era era,
        Dictionary<string, int> slugToIdMap)
    {
        Athlete athlete = participation.Athlete;
        DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);
        string biologicalSlug = AgeCategory.ResolveSlug(athlete.DateOfBirth, meetDate);
        IReadOnlyList<string> cascadeSlugs =
            AgeCategory.GetCascadeSlugs(biologicalSlug);

        List<int> cascadeAgeCategoryIds = [];

        foreach (string cascadeSlug in cascadeSlugs)
        {
            if (slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
            {
                cascadeAgeCategoryIds.Add(ageCategoryId);
            }
        }

        IReadOnlyList<Discipline> requiredDisciplines =
            MeetDisciplineResolver.ResolveDisciplines(
                meet.MeetType.MeetTypeId,
                meet.MeetType.Title);

        List<RecordCategory> applicableCategories = [];

        foreach (Discipline discipline in requiredDisciplines)
        {
            RecordCategory category =
                MeetDisciplineResolver.MapDisciplineToRecordCategory(
                    discipline,
                    meet.MeetType.MeetTypeId,
                    meet.MeetType.Title);

            if (category != RecordCategory.None)
            {
                applicableCategories.Add(category);

                RecordCategory? singleLiftCategory = category switch
                {
                    RecordCategory.Bench => RecordCategory.BenchSingle,
                    RecordCategory.Deadlift => RecordCategory.DeadliftSingle,
                    _ => null,
                };

                if (singleLiftCategory is not null)
                {
                    applicableCategories.Add(singleLiftCategory.Value);
                }
            }
        }

        if (requiredDisciplines.Count > 1 && participation.Total > 0)
        {
            applicableCategories.Add(RecordCategory.Total);
        }

        List<SlotKey> affectedSlots = [];

        foreach (RecordCategory category in applicableCategories)
        {
            foreach (int ageCategoryId in cascadeAgeCategoryIds)
            {
                affectedSlots.Add(new SlotKey(
                    era.EraId,
                    ageCategoryId,
                    participation.WeightCategoryId,
                    category,
                    meet.IsRaw));
            }
        }

        return affectedSlots;
    }

    private async Task HandleInvalidTotalAsync(
        Participation participation,
        Dictionary<string, int> slugToIdMap,
        List<Era> eras,
        CancellationToken cancellationToken)
    {
        List<int> participationAttemptIds = participation.Attempts
            .Select(a => a.AttemptId)
            .ToList();

        List<Record> existingRecords = await _dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => r.AttemptId != null)
            .Where(r => participationAttemptIds.Contains(r.AttemptId!.Value))
            .ToListAsync(cancellationToken);

        if (existingRecords.Count == 0)
        {
            _logger.LogInformation(
                "No valid total for participation {ParticipationId}, no records to revoke",
                participation.ParticipationId);
            return;
        }

        List<SlotKey> affectedSlots = existingRecords
            .Select(r => new SlotKey(
                r.EraId,
                r.AgeCategoryId,
                r.WeightCategoryId,
                r.RecordCategoryId,
                r.IsRaw))
            .Distinct()
            .ToList();

        List<int> recordIdsToDelete = existingRecords
            .Select(r => r.RecordId)
            .ToList();

        IExecutionStrategy strategy =
            _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.RepeatableRead,
                    cancellationToken);

            await _dbContext.Set<Record>()
                .Where(r => recordIdsToDelete.Contains(r.RecordId))
                .ExecuteDeleteAsync(cancellationToken);

            HashSet<int> weightCategoryIds = affectedSlots
                .Select(s => s.WeightCategoryId)
                .ToHashSet();

            HashSet<bool> isRawValues = affectedSlots
                .Select(s => s.IsRaw)
                .ToHashSet();

            List<Attempt> allAttempts = await _dbContext.Set<Attempt>()
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
                .Where(a => a.Participation.Athlete.Country.Iso3 == RecordConstants.IcelandIso3)
                .Where(a => weightCategoryIds.Contains(
                    a.Participation.WeightCategoryId))
                .Where(a => isRawValues.Contains(
                    a.Participation.Meet.IsRaw))
                .ToListAsync(cancellationToken);

            List<SlotAttempt> disciplineSlotAttempts =
                BuildSlotAttempts(allAttempts, eras, slugToIdMap);

            List<SlotAttempt> totalSlotAttempts =
                BuildTotalSlotAttempts(allAttempts, eras, slugToIdMap);

            foreach (SlotKey slot in affectedSlots)
            {
                string slotKey = FormatSlotKey(
                    slot.EraId,
                    slot.AgeCategoryId,
                    slot.WeightCategoryId,
                    slot.RecordCategory,
                    slot.IsRaw);

                List<SlotAttempt> slotAttempts =
                    slot.RecordCategory == RecordCategory.Total
                        ? FilterAndOrderForChain(totalSlotAttempts, slotKey)
                        : FilterAndOrderForChain(disciplineSlotAttempts, slotKey);

                StandardRecordInfo? standardRecord =
                    await GetLatestStandardRecordAsync(
                        slot.EraId,
                        slot.AgeCategoryId,
                        slot.WeightCategoryId,
                        slot.RecordCategory,
                        slot.IsRaw,
                        _dbContext,
                        cancellationToken);

                if (standardRecord is not null)
                {
                    slotAttempts = slotAttempts
                        .Where(sa => sa.MeetDate >= standardRecord.Date)
                        .ToList();
                }

                List<ExpectedRecord> expectedChain =
                    ComputeExpectedChain(
                        slotAttempts,
                        standardRecord?.Weight ?? 0m);

                SlotReconciliationResult result =
                    await ReconcileSlotAsync(
                        slot.EraId,
                        slot.AgeCategoryId,
                        slot.WeightCategoryId,
                        slot.RecordCategory,
                        slot.IsRaw,
                        expectedChain,
                        _dbContext,
                        cancellationToken);

                await ApplyReconciliationResultAsync(
                    result,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task FullRebuildSlotsAsync(
        IReadOnlyList<SlotKey> affectedSlots,
        List<Era> eras,
        Dictionary<string, int> slugToIdMap,
        CancellationToken cancellationToken)
    {
        HashSet<int> weightCategoryIds = affectedSlots
            .Select(s => s.WeightCategoryId)
            .ToHashSet();

        HashSet<bool> isRawValues = affectedSlots
            .Select(s => s.IsRaw)
            .ToHashSet();

        List<Attempt> allAttempts = await _dbContext.Set<Attempt>()
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
            .Where(a => a.Participation.Athlete.Country.Iso3 == RecordConstants.IcelandIso3)
            .Where(a => weightCategoryIds.Contains(
                a.Participation.WeightCategoryId))
            .Where(a => isRawValues.Contains(a.Participation.Meet.IsRaw))
            .ToListAsync(cancellationToken);

        List<SlotAttempt> disciplineSlotAttempts =
            BuildSlotAttempts(allAttempts, eras, slugToIdMap);

        List<SlotAttempt> totalSlotAttempts =
            BuildTotalSlotAttempts(allAttempts, eras, slugToIdMap);

        IExecutionStrategy strategy =
            _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.RepeatableRead,
                    cancellationToken);

            await RebuildSlotsContentAsync(
                affectedSlots,
                disciplineSlotAttempts,
                totalSlotAttempts,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task RebuildSlotsContentAsync(
        IReadOnlyList<SlotKey> affectedSlots,
        List<Era> eras,
        Dictionary<string, int> slugToIdMap,
        CancellationToken cancellationToken)
    {
        HashSet<int> weightCategoryIds = affectedSlots
            .Select(s => s.WeightCategoryId)
            .ToHashSet();

        HashSet<bool> isRawValues = affectedSlots
            .Select(s => s.IsRaw)
            .ToHashSet();

        List<Attempt> allAttempts = await _dbContext.Set<Attempt>()
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
            .Where(a => a.Participation.Athlete.Country.Iso3 == RecordConstants.IcelandIso3)
            .Where(a => weightCategoryIds.Contains(
                a.Participation.WeightCategoryId))
            .Where(a => isRawValues.Contains(a.Participation.Meet.IsRaw))
            .ToListAsync(cancellationToken);

        List<SlotAttempt> disciplineSlotAttempts =
            BuildSlotAttempts(allAttempts, eras, slugToIdMap);

        List<SlotAttempt> totalSlotAttempts =
            BuildTotalSlotAttempts(allAttempts, eras, slugToIdMap);

        await RebuildSlotsContentAsync(
            affectedSlots,
            disciplineSlotAttempts,
            totalSlotAttempts,
            cancellationToken);
    }

    private async Task RebuildSlotsContentAsync(
        IReadOnlyList<SlotKey> affectedSlots,
        List<SlotAttempt> disciplineSlotAttempts,
        List<SlotAttempt> totalSlotAttempts,
        CancellationToken cancellationToken)
    {
        foreach (SlotKey slot in affectedSlots)
        {
            string slotKey = FormatSlotKey(
                slot.EraId,
                slot.AgeCategoryId,
                slot.WeightCategoryId,
                slot.RecordCategory,
                slot.IsRaw);

            List<SlotAttempt> slotAttempts =
                slot.RecordCategory == RecordCategory.Total
                    ? FilterAndOrderForChain(totalSlotAttempts, slotKey)
                    : FilterAndOrderForChain(disciplineSlotAttempts, slotKey);

            StandardRecordInfo? standardRecord =
                await GetLatestStandardRecordAsync(
                    slot.EraId,
                    slot.AgeCategoryId,
                    slot.WeightCategoryId,
                    slot.RecordCategory,
                    slot.IsRaw,
                    _dbContext,
                    cancellationToken);

            if (standardRecord is not null)
            {
                slotAttempts = slotAttempts
                    .Where(sa => sa.MeetDate >= standardRecord.Date)
                    .ToList();
            }

            List<ExpectedRecord> expectedChain =
                ComputeExpectedChain(
                    slotAttempts,
                    standardRecord?.Weight ?? 0m);

            SlotReconciliationResult result =
                await ReconcileSlotAsync(
                    slot.EraId,
                    slot.AgeCategoryId,
                    slot.WeightCategoryId,
                    slot.RecordCategory,
                    slot.IsRaw,
                    expectedChain,
                    _dbContext,
                    cancellationToken);

            await ApplyReconciliationResultAsync(
                result,
                cancellationToken);
        }
    }

    private async Task ApplyReconciliationResultAsync(
        SlotReconciliationResult result,
        CancellationToken cancellationToken)
    {
        if (result.RecordIdsToDelete.Count > 0)
        {
            await _dbContext.Set<Record>()
                .Where(r => result.RecordIdsToDelete.Contains(r.RecordId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.RecordIdsToDemote.Count > 0)
        {
            await _dbContext.Set<Record>()
                .Where(r => result.RecordIdsToDemote.Contains(r.RecordId))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.IsCurrent, false),
                    cancellationToken);
        }

        if (result.RecordIdsToSetCurrent.Count > 0)
        {
            await _dbContext.Set<Record>()
                .Where(r => result.RecordIdsToSetCurrent.Contains(
                    r.RecordId))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.IsCurrent, true),
                    cancellationToken);
        }

        foreach ((int recordId, decimal weight) in result.WeightUpdates)
        {
            await _dbContext.Set<Record>()
                .Where(r => r.RecordId == recordId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.Weight, weight),
                    cancellationToken);
        }

        if (result.RecordsToCreate.Count > 0)
        {
            _dbContext.Set<Record>().AddRange(result.RecordsToCreate);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
        }
    }
}