using System.Data;
using System.Text.RegularExpressions;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KRAFT.Results.WebApi.Features.Clubs.Delete;

internal sealed partial class DeleteClubHandler
{
    private const int SlugMaxLength = 200;

    private readonly ILogger<DeleteClubHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public DeleteClubHandler(ILogger<DeleteClubHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug) || slug.Length > SlugMaxLength || !ValidSlugPattern().IsMatch(slug))
        {
            return Result.Failure(ClubErrors.ClubNotFound);
        }

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            Club? club = await _dbContext.Set<Club>()
                .Where(t => t.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (club is null)
            {
                _logger.LogWarning("Club with slug '{Slug}' was not found", slug);
                return Result.Failure(ClubErrors.ClubNotFound);
            }

            bool hasAthletes = await _dbContext.Set<Athlete>()
                .AnyAsync(a => a.TeamId == club.ClubId, cancellationToken);

            if (hasAthletes)
            {
                _logger.LogWarning("Cannot delete club '{Slug}' because it has athletes assigned", slug);
                return Result.Failure(ClubErrors.ClubHasAthletes);
            }

            _dbContext.Set<Club>().Remove(club);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        });
    }

    [GeneratedRegex(@"^[a-z0-9-]+$")]
    private static partial Regex ValidSlugPattern();
}
