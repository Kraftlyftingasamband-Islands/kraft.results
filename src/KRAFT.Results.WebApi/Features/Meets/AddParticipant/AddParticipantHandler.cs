using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.EraWeightCategories;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Features.WeightCategories;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.AddParticipant;

internal sealed class AddParticipantHandler
{
    private readonly ILogger<AddParticipantHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public AddParticipantHandler(ILogger<AddParticipantHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result<int>> Handle(int meetId, AddParticipantCommand command, CancellationToken cancellationToken)
    {
        Result<User> creatorResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (creatorResult.IsFailure)
        {
            return new Result<int>(creatorResult.Error);
        }

        User creator = creatorResult.FromResult();

        Athlete? athlete = await _dbContext.Set<Athlete>()
            .FirstOrDefaultAsync(a => a.Slug == command.AthleteSlug, cancellationToken);

        if (athlete is null)
        {
            _logger.LogWarning("Athlete with slug {AthleteSlug} was not found", command.AthleteSlug);
            return new Result<int>(AthleteErrors.AthleteNotFound);
        }

        Meet? meet = await _dbContext.Set<Meet>()
            .Where(m => EF.Property<int>(m, "MeetId") == meetId)
            .FirstOrDefaultAsync(cancellationToken);

        if (meet is null)
        {
            _logger.LogWarning("Meet with Id {MeetId} was not found", meetId);
            return new Result<int>(MeetErrors.MeetNotFound);
        }

        if (command.TeamId is not null)
        {
            bool teamExists = await _dbContext.Set<Team>()
                .AnyAsync(t => t.TeamId == command.TeamId, cancellationToken);

            if (!teamExists)
            {
                _logger.LogWarning("Team with Id {TeamId} was not found", command.TeamId);
                return new Result<int>(TeamErrors.TeamNotFound);
            }
        }

        if (command.BodyWeight <= 0)
        {
            return new Result<int>(ParticipationErrors.BodyWeightMustBePositive);
        }

        if (command.BodyWeight > Participation.MaxBodyWeight)
        {
            return new Result<int>(ParticipationErrors.BodyWeightTooHigh);
        }

        bool alreadyRegistered = await _dbContext.Set<Participation>()
            .AnyAsync(p => p.MeetId == meetId && p.AthleteId == athlete.AthleteId, cancellationToken);

        if (alreadyRegistered)
        {
            _logger.LogWarning("Athlete {AthleteId} is already registered in meet {MeetId}", athlete.AthleteId, meetId);
            return new Result<int>(MeetErrors.AthleteAlreadyRegistered);
        }

        DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);
        string ageSlug = AgeCategory.ResolveSlug(athlete.DateOfBirth, meetDate);

        if (!string.IsNullOrEmpty(command.AgeCategorySlug))
        {
            IReadOnlyList<string> eligibleSlugs = AgeCategory.ResolveEligibleSlugs(athlete.DateOfBirth, meetDate);
            if (eligibleSlugs.Contains(command.AgeCategorySlug))
            {
                ageSlug = command.AgeCategorySlug;
            }
        }

        List<WeightCategory> eligibleCategories = await _dbContext.Set<EraWeightCategory>()
            .Where(ewc => (ewc.FromDate == null || ewc.FromDate <= meet.StartDate)
                       && (ewc.ToDate == null || ewc.ToDate >= meet.StartDate))
            .Select(ewc => ewc.WeightCategory)
            .Where(wc => wc.Gender == athlete.Gender)
            .Where(wc => !wc.JuniorsOnly || ageSlug == "subjunior" || ageSlug == "junior")
            .ToListAsync(cancellationToken);

        WeightCategory? weightCategory = WeightCategory.FindBestFit(eligibleCategories, command.BodyWeight);

        if (weightCategory is null)
        {
            _logger.LogWarning("No matching weight category for body weight {BodyWeight}", command.BodyWeight);
            return new Result<int>(MeetErrors.NoMatchingWeightCategory);
        }

        AgeCategory? ageCategory = await _dbContext.Set<AgeCategory>()
            .FirstOrDefaultAsync(ac => ac.Slug == ageSlug, cancellationToken);

        if (ageCategory is null)
        {
            ageCategory = await _dbContext.Set<AgeCategory>()
                .FirstOrDefaultAsync(ac => ac.Slug == "open", cancellationToken);
        }

        Result<Participation> participationResult = Participation.Create(
            creator,
            athlete.AthleteId,
            meetId,
            weightCategory.WeightCategoryId,
            ageCategory!.AgeCategoryId,
            command.BodyWeight,
            command.TeamId);

        if (participationResult.IsFailure)
        {
            return new Result<int>(participationResult.Error);
        }

        Participation participation = participationResult.FromResult();

        _dbContext.Set<Participation>().Add(participation);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added athlete {AthleteSlug} to meet {MeetId} with participation {ParticipationId}", command.AthleteSlug, meetId, participation.ParticipationId);

        return participation.ParticipationId;
    }
}