using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records.ComputeRecords;

internal sealed class RecordComputationService(
    ResultsDbContext dbContext,
    ILogger<RecordComputationService> logger)
{
    private const string CreatedBySystem = "system";

    private readonly ResultsDbContext _dbContext = dbContext;
    private readonly ILogger<RecordComputationService> _logger = logger;

    internal async Task ComputeRecordsAsync(int attemptId, CancellationToken cancellationToken)
    {
        Attempt? attempt = await _dbContext.Set<Attempt>()
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

        if (!attempt.Good || attempt.Weight <= 0)
        {
            return;
        }

        RecordCategory recordCategory = MapDisciplineToRecordCategory(
            attempt.Discipline,
            meet.MeetType.MeetTypeId);

        if (recordCategory == RecordCategory.None)
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
            .Where(ac => ac.Slug != null)
            .ToDictionaryAsync(ac => ac.Slug!, ac => ac.AgeCategoryId, cancellationToken);

        foreach (string cascadeSlug in cascadeSlugs)
        {
            if (!slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
            {
                continue;
            }

            bool beatRecord = await TrySetRecordAsync(
                era.EraId,
                ageCategoryId,
                participation.WeightCategoryId,
                recordCategory,
                meet.IsRaw,
                attempt.Weight,
                meetDate,
                attemptId,
                cancellationToken);

            if (!beatRecord)
            {
                break;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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

    private async Task<bool> TrySetRecordAsync(
        int eraId,
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory recordCategory,
        bool isRaw,
        decimal weight,
        DateOnly date,
        int attemptId,
        CancellationToken cancellationToken)
    {
        Record? currentRecord = await _dbContext.Set<Record>()
            .Where(r => r.EraId == eraId)
            .Where(r => r.AgeCategoryId == ageCategoryId)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == recordCategory)
            .Where(r => r.IsRaw == isRaw)
            .Where(r => r.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);

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