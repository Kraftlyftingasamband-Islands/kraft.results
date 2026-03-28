using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Participations;
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
        User creator = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        Result? validationError = await ValidateAsync(meetId, command.AthleteId, command.WeightCategoryId, cancellationToken);

        if (validationError is not null)
        {
            return new Result<int>(validationError.Error);
        }

        Participation participation = Participation.Create(
            creator,
            command.AthleteId,
            meetId,
            command.WeightCategoryId,
            command.BodyWeight ?? 0);

        _dbContext.Set<Participation>().Add(participation);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added athlete {AthleteId} to meet {MeetId} with participation {ParticipationId}", command.AthleteId, meetId, participation.ParticipationId);

        return participation.ParticipationId;
    }

    private async Task<Result?> ValidateAsync(int meetId, int athleteId, int weightCategoryId, CancellationToken cancellationToken)
    {
        var existence = await _dbContext.Set<Meet>()
            .Where(m => EF.Property<int>(m, "MeetId") == meetId)
            .Select(m => new
            {
                MeetExists = true,
                AthleteExists = _dbContext.Set<Athlete>()
                    .Any(a => a.AthleteId == athleteId),
                WeightCategoryExists = _dbContext.Set<WeightCategory>()
                    .Any(w => w.WeightCategoryId == weightCategoryId),
                AlreadyRegistered = _dbContext.Set<Participation>()
                    .Any(p => p.MeetId == meetId && p.AthleteId == athleteId),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existence is null)
        {
            _logger.LogWarning("Meet with Id {MeetId} was not found", meetId);
            return Result.Failure(MeetErrors.MeetNotFound);
        }

        if (!existence.AthleteExists)
        {
            _logger.LogWarning("Athlete with Id {AthleteId} was not found", athleteId);
            return Result.Failure(AthleteErrors.AthleteNotFound);
        }

        if (!existence.WeightCategoryExists)
        {
            _logger.LogWarning("Weight category with Id {WeightCategoryId} was not found", weightCategoryId);
            return Result.Failure(MeetErrors.WeightCategoryNotFound);
        }

        if (existence.AlreadyRegistered)
        {
            _logger.LogWarning("Athlete {AthleteId} is already registered in meet {MeetId}", athleteId, meetId);
            return Result.Failure(MeetErrors.AthleteAlreadyRegistered);
        }

        return null;
    }
}