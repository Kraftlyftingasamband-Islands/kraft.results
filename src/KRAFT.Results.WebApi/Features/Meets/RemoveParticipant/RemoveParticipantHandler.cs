using System.Data;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KRAFT.Results.WebApi.Features.Meets.RemoveParticipant;

internal sealed class RemoveParticipantHandler
{
    private readonly ILogger<RemoveParticipantHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly RecordComputationService _recordComputationService;

    public RemoveParticipantHandler(
        ILogger<RemoveParticipantHandler> logger,
        ResultsDbContext dbContext,
        RecordComputationService recordComputationService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _recordComputationService = recordComputationService;
    }

    public async Task<Result> Handle(int meetId, int participationId, CancellationToken cancellationToken)
    {
        Participation? participation = await _dbContext.Set<Participation>()
            .Include(p => p.Attempts)
            .Where(p => p.ParticipationId == participationId)
            .Where(p => p.MeetId == meetId)
            .FirstOrDefaultAsync(cancellationToken);

        if (participation is null)
        {
            _logger.LogWarning("Participation {ParticipationId} in meet {MeetId} was not found", participationId, meetId);
            return Result.Failure(MeetErrors.ParticipationNotFound);
        }

        List<int> attemptIds = participation.Attempts
            .Select(a => a.AttemptId)
            .ToList();

        List<SlotKey> affectedSlots = await _dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => r.AttemptId != null)
            .Where(r => attemptIds.Contains(r.AttemptId!.Value))
            .Select(r => new SlotKey(r.EraId, r.AgeCategoryId, r.WeightCategoryId, r.RecordCategoryId, r.IsRaw))
            .Distinct()
            .ToListAsync(cancellationToken);

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.RepeatableRead,
                    cancellationToken);

            List<Record> referencingRecords = await _dbContext.Set<Record>()
                .Where(r => r.AttemptId != null)
                .Where(r => attemptIds.Contains(r.AttemptId!.Value))
                .ToListAsync(cancellationToken);

            _dbContext.Set<Record>().RemoveRange(referencingRecords);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.Set<Participation>().Remove(participation);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (affectedSlots.Count > 0)
            {
                await _recordComputationService.RebuildSlotsWithinTransactionAsync(
                    affectedSlots,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });

        _logger.LogInformation("Removed participation {ParticipationId} from meet {MeetId}", participationId, meetId);

        return Result.Success();
    }
}