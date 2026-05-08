using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Participations.ComputePlaces;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed class BanEventHandler(
    ResultsDbContext dbContext,
    PlaceComputationService placeComputationService,
    RecordComputationService recordComputationService,
    ILogger<BanEventHandler> logger) : IDomainEventHandler<BanAddedEvent>, IDomainEventHandler<BanRemovedEvent>
{
    private readonly ResultsDbContext _dbContext = dbContext;
    private readonly PlaceComputationService _placeComputationService = placeComputationService;
    private readonly RecordComputationService _recordComputationService = recordComputationService;
    private readonly ILogger<BanEventHandler> _logger = logger;

    public async Task HandleAsync(BanAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing BanAddedEvent for athlete {AthleteId}, ban period {FromDate} to {ToDate}",
            domainEvent.AthleteId,
            domainEvent.FromDate,
            domainEvent.ToDate);

        await HandleBanEventAsync(
            domainEvent.AthleteId,
            domainEvent.FromDate,
            domainEvent.ToDate,
            cancellationToken);
    }

    public async Task HandleAsync(BanRemovedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing BanRemovedEvent for athlete {AthleteId}, ban period {FromDate} to {ToDate}",
            domainEvent.AthleteId,
            domainEvent.FromDate,
            domainEvent.ToDate);

        await HandleBanEventAsync(
            domainEvent.AthleteId,
            domainEvent.FromDate,
            domainEvent.ToDate,
            cancellationToken);
    }

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is BanAddedEvent banAddedEvent)
        {
            return HandleAsync(banAddedEvent, cancellationToken);
        }

        if (domainEvent is BanRemovedEvent banRemovedEvent)
        {
            return HandleAsync(banRemovedEvent, cancellationToken);
        }

        return Task.CompletedTask;
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
        IReadOnlyList<string> cascadeSlugs = AgeCategory.GetCascadeSlugs(biologicalSlug);

        List<int> cascadeAgeCategoryIds = [];

        foreach (string cascadeSlug in cascadeSlugs)
        {
            if (slugToIdMap.TryGetValue(cascadeSlug, out int ageCategoryId))
            {
                cascadeAgeCategoryIds.Add(ageCategoryId);
            }
        }

        IReadOnlyList<Discipline> requiredDisciplines = meet.Category.GetDisciplines();

        List<RecordCategory> applicableCategories = [];

        foreach (Discipline discipline in requiredDisciplines)
        {
            RecordCategory category = meet.Category.MapDisciplineToRecordCategory(discipline);

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

    private async Task HandleBanEventAsync(
        int athleteId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        List<Participation> affectedParticipations = await _dbContext.Set<Participation>()
            .Include(p => p.Meet)
            .Include(p => p.Athlete)
                .ThenInclude(a => a.Bans)
            .Include(p => p.Attempts)
            .Include(p => p.AgeCategory)
            .Where(p => p.AthleteId == athleteId)
            .Where(p => p.Meet.StartDate >= fromDate)
            .Where(p => p.Meet.StartDate <= toDate)
            .Where(p => p.Attempts.Any())
            .ToListAsync(cancellationToken);

        if (affectedParticipations.Count == 0)
        {
            _logger.LogInformation(
                "No affected participations found for athlete {AthleteId} in ban period {FromDate} to {ToDate}",
                athleteId,
                fromDate,
                toDate);
            return;
        }

        List<Era> eras = await _dbContext.Set<Era>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<string, int> slugToIdMap = await _dbContext.Set<AgeCategory>()
            .Where(ac => ac.Slug != null)
            .ToDictionaryAsync(
                ac => ac.Slug!,
                ac => ac.AgeCategoryId,
                cancellationToken);

        List<SlotKey> allAffectedSlots = [];

        foreach (Participation participation in affectedParticipations)
        {
            participation.RecalculateTotals();

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _placeComputationService.ComputePlacesAsync(participation, cancellationToken);

            Meet meet = participation.Meet;
            DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);

            Era? era = eras.FirstOrDefault(
                e => e.StartDate <= meetDate && e.EndDate >= meetDate);

            if (era is null)
            {
                _logger.LogWarning(
                    "No era found for meet date {MeetDate} (ParticipationId: {ParticipationId})",
                    meetDate,
                    participation.ParticipationId);
                continue;
            }

            List<SlotKey> participationSlots = DetermineAffectedSlots(
                participation,
                meet,
                era,
                slugToIdMap);

            allAffectedSlots.AddRange(participationSlots);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        List<SlotKey> distinctSlots = allAffectedSlots.Distinct().ToList();

        if (distinctSlots.Count > 0)
        {
            await _recordComputationService.RebuildSlotsAsync(distinctSlots, cancellationToken);
        }
    }
}