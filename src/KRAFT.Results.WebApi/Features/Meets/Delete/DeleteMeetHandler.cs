using System.Data;
using System.Text.RegularExpressions;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KRAFT.Results.WebApi.Features.Meets.Delete;

internal sealed partial class DeleteMeetHandler
{
    private const int SlugMaxLength = 200;

    private readonly ILogger<DeleteMeetHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public DeleteMeetHandler(ILogger<DeleteMeetHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug) || slug.Length > SlugMaxLength || !ValidSlugPattern().IsMatch(slug))
        {
            return Result.Failure(MeetErrors.MeetNotFound);
        }

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            Meet? meet = await _dbContext.Set<Meet>()
                .Where(m => m.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (meet is null)
            {
                _logger.LogWarning("Meet with slug '{Slug}' was not found", slug);
                return Result.Failure(MeetErrors.MeetNotFound);
            }

            int meetId = _dbContext.Entry(meet).Property<int>("MeetId").CurrentValue;
            bool hasParticipations = await _dbContext.Set<Participation>()
                .AnyAsync(p => p.MeetId == meetId, cancellationToken);

            if (hasParticipations)
            {
                _logger.LogWarning("Cannot delete meet '{Slug}' because it has participations", slug);
                return Result.Failure(MeetErrors.MeetHasParticipations);
            }

            _dbContext.Set<Meet>().Remove(meet);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        });
    }

    [GeneratedRegex(@"^[a-z0-9-]+$")]
    private static partial Regex ValidSlugPattern();
}