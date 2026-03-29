using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.RemoveParticipant;

internal sealed class RemoveParticipantHandler
{
    private readonly ILogger<RemoveParticipantHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public RemoveParticipantHandler(ILogger<RemoveParticipantHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(int meetId, int participationId, CancellationToken cancellationToken)
    {
        Participation? participation = await _dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationId && p.MeetId == meetId)
            .FirstOrDefaultAsync(cancellationToken);

        if (participation is null)
        {
            _logger.LogWarning("Participation {ParticipationId} in meet {MeetId} was not found", participationId, meetId);
            return Result.Failure(MeetErrors.ParticipationNotFound);
        }

        _dbContext.Set<Participation>().Remove(participation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed participation {ParticipationId} from meet {MeetId}", participationId, meetId);

        return Result.Success();
    }
}