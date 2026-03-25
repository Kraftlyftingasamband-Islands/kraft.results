using System.Data;
using System.Text.RegularExpressions;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KRAFT.Results.WebApi.Features.Athletes.Delete;

internal sealed partial class DeleteAthleteHandler
{
    private const int SlugMaxLength = 200;

    private readonly ILogger<DeleteAthleteHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public DeleteAthleteHandler(ILogger<DeleteAthleteHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug) || slug.Length > SlugMaxLength || !ValidSlugPattern().IsMatch(slug))
        {
            return Result.Failure(AthleteErrors.AthleteNotFound);
        }

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            Athlete? athlete = await _dbContext.Set<Athlete>()
                .Where(a => a.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (athlete is null)
            {
                _logger.LogWarning("Athlete with slug '{Slug}' was not found", slug);
                return Result.Failure(AthleteErrors.AthleteNotFound);
            }

            bool hasParticipations = await _dbContext.Set<Participation>()
                .AnyAsync(p => p.AthleteId == athlete.AthleteId, cancellationToken);

            if (hasParticipations)
            {
                _logger.LogWarning("Cannot delete athlete with slug '{Slug}' because they have participations", slug);
                return Result.Failure(AthleteErrors.AthleteHasParticipations);
            }

            _dbContext.Set<Athlete>().Remove(athlete);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        });
    }

    [GeneratedRegex(@"^[a-z0-9-]+$")]
    private static partial Regex ValidSlugPattern();
}