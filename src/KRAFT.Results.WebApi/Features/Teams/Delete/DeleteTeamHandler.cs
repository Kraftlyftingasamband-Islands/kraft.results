using System.Data;
using System.Text.RegularExpressions;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KRAFT.Results.WebApi.Features.Teams.Delete;

internal sealed partial class DeleteTeamHandler
{
    private const int SlugMaxLength = 200;

    private readonly ILogger<DeleteTeamHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public DeleteTeamHandler(ILogger<DeleteTeamHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug) || slug.Length > SlugMaxLength || !ValidSlugPattern().IsMatch(slug))
        {
            return Result.Failure(TeamErrors.TeamNotFound);
        }

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            Team? team = await _dbContext.Set<Team>()
                .Where(t => t.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (team is null)
            {
                _logger.LogWarning("Team with slug '{Slug}' was not found", slug);
                return Result.Failure(TeamErrors.TeamNotFound);
            }

            bool hasAthletes = await _dbContext.Set<Athlete>()
                .AnyAsync(a => a.TeamId == team.TeamId, cancellationToken);

            if (hasAthletes)
            {
                _logger.LogWarning("Cannot delete team '{Slug}' because it has athletes assigned", slug);
                return Result.Failure(TeamErrors.TeamHasAthletes);
            }

            _dbContext.Set<Team>().Remove(team);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        });
    }

    [GeneratedRegex(@"^[a-z0-9-]+$")]
    private static partial Regex ValidSlugPattern();
}