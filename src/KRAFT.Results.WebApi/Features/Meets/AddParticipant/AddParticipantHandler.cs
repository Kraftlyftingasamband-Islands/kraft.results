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

        if (!await MeetExistsAsync(meetId, cancellationToken))
        {
            _logger.LogWarning("Meet with Id {MeetId} was not found", meetId);
            return MeetErrors.MeetNotFound;
        }

        if (!await AthleteExistsAsync(command.AthleteId, cancellationToken))
        {
            _logger.LogWarning("Athlete with Id {AthleteId} was not found", command.AthleteId);
            return AthleteErrors.AthleteNotFound;
        }

        if (!await WeightCategoryExistsAsync(command.WeightCategoryId, cancellationToken))
        {
            _logger.LogWarning("Weight category with Id {WeightCategoryId} was not found", command.WeightCategoryId);
            return MeetErrors.WeightCategoryNotFound;
        }

        if (await IsAlreadyRegisteredAsync(meetId, command.AthleteId, cancellationToken))
        {
            _logger.LogWarning("Athlete {AthleteId} is already registered in meet {MeetId}", command.AthleteId, meetId);
            return MeetErrors.AthleteAlreadyRegistered;
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

    private Task<bool> MeetExistsAsync(int meetId, CancellationToken cancellationToken) =>
        _dbContext.Set<Meet>()
            .AnyAsync(m => EF.Property<int>(m, "MeetId") == meetId, cancellationToken);

    private Task<bool> AthleteExistsAsync(int athleteId, CancellationToken cancellationToken) =>
        _dbContext.Set<Athlete>()
            .AnyAsync(a => a.AthleteId == athleteId, cancellationToken);

    private Task<bool> WeightCategoryExistsAsync(int weightCategoryId, CancellationToken cancellationToken) =>
        _dbContext.Set<WeightCategory>()
            .AnyAsync(w => w.WeightCategoryId == weightCategoryId, cancellationToken);

    private Task<bool> IsAlreadyRegisteredAsync(int meetId, int athleteId, CancellationToken cancellationToken) =>
        _dbContext.Set<Participation>()
            .AnyAsync(p => p.MeetId == meetId && p.AthleteId == athleteId, cancellationToken);
}