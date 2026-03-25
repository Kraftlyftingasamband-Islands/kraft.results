using System.Data;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KRAFT.Results.WebApi.Features.Athletes.Delete;

internal sealed class DeleteAthleteHandler
{
    private readonly ILogger<DeleteAthleteHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public DeleteAthleteHandler(ILogger<DeleteAthleteHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Result.Failure(AthleteErrors.AthleteNotFound);
        }

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            Athlete? athlete = await _dbContext.Set<Athlete>()
                .Where(a => a.AthleteId == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (athlete is null)
            {
                _logger.LogWarning("Athlete with id '{AthleteId}' was not found", id);
                return Result.Failure(AthleteErrors.AthleteNotFound);
            }

            bool hasParticipations = await _dbContext.Set<Participation>()
                .AnyAsync(p => p.AthleteId == id, cancellationToken);

            if (hasParticipations)
            {
                _logger.LogWarning("Cannot delete athlete with id '{AthleteId}' because they have participations", id);
                return Result.Failure(AthleteErrors.AthleteHasParticipations);
            }

            _dbContext.Set<Athlete>().Remove(athlete);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        });
    }
}