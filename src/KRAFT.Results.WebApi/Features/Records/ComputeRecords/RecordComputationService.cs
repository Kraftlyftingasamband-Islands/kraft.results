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
    private const string CreatedBySystem = "system";
    private const int IcelandCountryId = 1;

    private readonly ResultsDbContext _dbContext = dbContext;
    private readonly ILogger<RecordComputationService> _logger = logger;

    internal async Task ComputeRecordsAsync(int attemptId, CancellationToken cancellationToken)
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

        if (athlete.CountryId != IcelandCountryId)
        {
            return;
        }

        Era? era = await FindEraForDateAsync(meetDate, cancellationToken);

        if (era is null)
        {
            _logger.LogWarning(
                "Skipping record computation: no era found for date {MeetDate} (AttemptId: {AttemptId})",
                meetDate,
                attemptId);
            return;
        }

        if (!HasValidTotal(participation, meet))
        {
            _logger.LogWarning(
                "Skipping record computation: no valid total for participation {ParticipationId} (AttemptId: {AttemptId})",
                participation.ParticipationId,
                attemptId);
            return;
        }

        IReadOnlyList<Discipline> requiredDisciplines = MeetDisciplineResolver.ResolveDisciplines(
            meet.MeetType.MeetTypeId,
            meet.MeetType.Title);

        List<(RecordCategory Category, decimal Weight, int AttemptId)> entries = [];

        foreach (Discipline discipline in requiredDisciplines)
        {
            Attempt? best = participation.Attempts
                .Where(a => a.Discipline == discipline)
                .Where(a => a.Good)
                .Where(a => a.Weight > 0)
                .OrderByDescending(a => a.Weight)
                .ThenByDescending(a => a.AttemptId)
                .FirstOrDefault();

            if (best is null)
            {
                continue;
            }

            RecordCategory category = MeetDisciplineResolver.MapDisciplineToRecordCategory(
                discipline,
                meet.MeetType.MeetTypeId,
                meet.MeetType.Title);

            if (category == RecordCategory.None)
            {
                continue;
            }

            entries.Add((category, best.Weight, best.AttemptId));
        }

        if (requiredDisciplines.Count > 1 && participation.Total > 0)
        {
            Attempt? bestDeadlift = participation.Attempts
                .Where(a => a.Discipline == Discipline.Deadlift)
                .Where(a => a.Good)
                .Where(a => a.Weight > 0)
                .OrderByDescending(a => a.Weight)
                .ThenByDescending(a => a.AttemptId)
                .FirstOrDefault();

            if (bestDeadlift is not null)
            {
                entries.Add((RecordCategory.Total, participation.Total, bestDeadlift.AttemptId));
            }
        }

        if (entries.Count == 0)
        {
            return;
        }

        AgeCategory ageCategory = participation.AgeCategory;
        string? slug = ageCategory.Slug;

        if (string.IsNullOrEmpty(slug))
        {
            return;
        }

        IReadOnlyList<string> cascadeSlugs = AgeCategory.GetCascadeSlugs(slug);

        Dictionary<string, int> slugToIdMap = await _dbContext.Set<AgeCategory>()
            .Where(ac => cascadeSlugs.Contains(ac.Slug!))
            .ToDictionaryAsync(ac => ac.Slug!, ac => ac.AgeCategoryId, cancellationToken);

        List<int> cascadeAgeCategoryIds = slugToIdMap.Values.ToList();
        List<RecordCategory> applicableCategories = entries
            .Select(e => e.Category)
            .ToList();

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

            List<Record> existingSlotRecords = await _dbContext.Set<Record>()
                .Where(r => r.EraId == era.EraId)
                .Where(r => cascadeAgeCategoryIds.Contains(r.AgeCategoryId))
                .Where(r => r.WeightCategoryId == participation.WeightCategoryId)
                .Where(r => applicableCategories.Contains(r.RecordCategoryId))
                .Where(r => r.IsRaw == meet.IsRaw)
                .Where(r => r.IsCurrent)
                .ToListAsync(cancellationToken);

            foreach ((RecordCategory category, decimal weight, int entryAttemptId) in entries)
            {
                List<Record> categoryRecords = existingSlotRecords
                    .Where(r => r.RecordCategoryId == category)
                    .ToList();

                foreach (string cascadeSlug in cascadeSlugs)
                {
                    if (!slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
                    {
                        continue;
                    }

                    bool beatRecord = TrySetRecord(
                        categoryRecords,
                        era.EraId,
                        ageCategoryId,
                        participation.WeightCategoryId,
                        category,
                        meet.IsRaw,
                        weight,
                        meetDate,
                        entryAttemptId);

                    if (!beatRecord)
                    {
                        break;
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private bool TrySetRecord(
        List<Record> existingSlotRecords,
        int eraId,
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory recordCategory,
        bool isRaw,
        decimal weight,
        DateOnly date,
        int attemptId)
    {
        Record? currentRecord = existingSlotRecords
            .FirstOrDefault(r => r.AgeCategoryId == ageCategoryId);

        if (currentRecord is not null && weight <= currentRecord.Weight)
        {
            return false;
        }

        if (currentRecord is not null)
        {
            currentRecord.Demote();
            _logger.LogInformation(
                "Record demoted: {EraId}/{AgeCategoryId}/{WeightCategoryId}/{RecordCategory}/{IsRaw} = {Weight}kg (RecordId: {RecordId})",
                currentRecord.EraId,
                currentRecord.AgeCategoryId,
                currentRecord.WeightCategoryId,
                currentRecord.RecordCategoryId,
                currentRecord.IsRaw,
                currentRecord.Weight,
                currentRecord.RecordId);
        }

        Record newRecord = Record.Create(
            eraId,
            ageCategoryId,
            weightCategoryId,
            recordCategory,
            weight,
            date,
            attemptId,
            isRaw,
            CreatedBySystem);

        newRecord.SetCurrent();

        _dbContext.Set<Record>().Add(newRecord);

        _logger.LogInformation(
            "Record set: {EraId}/{AgeCategoryId}/{WeightCategoryId}/{RecordCategory}/{IsRaw} = {Weight}kg (AttemptId: {AttemptId})",
            eraId,
            ageCategoryId,
            weightCategoryId,
            recordCategory,
            isRaw,
            weight,
            attemptId);

        return true;
    }

    private async Task<Era?> FindEraForDateAsync(DateOnly meetDate, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Era>()
            .Where(e => e.StartDate <= meetDate)
            .FirstOrDefaultAsync(e => e.EndDate >= meetDate, cancellationToken);
    }
}