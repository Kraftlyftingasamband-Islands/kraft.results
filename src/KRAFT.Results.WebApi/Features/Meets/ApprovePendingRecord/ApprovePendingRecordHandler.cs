using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.ApprovePendingRecord;

internal sealed class ApprovePendingRecordHandler(
    ResultsDbContext dbContext,
    IHttpContextService httpContextService)
{
    internal const string AttemptNotFoundCode = "Meets.AttemptNotFound";
    internal const string NotGoodLiftCode = "Meets.NotGoodLift";
    internal const string AlreadyHasRecordCode = "Meets.AlreadyHasRecord";
    internal const string NoEraFoundCode = "Meets.NoEraFound";

    private static readonly Error AttemptNotFound = new(
        AttemptNotFoundCode,
        "Attempt not found in the specified meet.");

    private static readonly Error NotGoodLift = new(
        NotGoodLiftCode,
        "Only good lifts can be approved as records.");

    private static readonly Error AlreadyHasRecord = new(
        AlreadyHasRecordCode,
        "A record already exists for this attempt.");

    private static readonly Error NoEraFound = new(
        NoEraFoundCode,
        "No era found for the meet date.");

    public async Task<Result> Handle(string slug, int attemptId, CancellationToken cancellationToken)
    {
        Attempt? attempt = await dbContext.Set<Attempt>()
            .Include(a => a.Participation)
                .ThenInclude(p => p.Meet)
            .FirstOrDefaultAsync(
                a => a.AttemptId == attemptId && a.Participation.Meet.Slug == slug,
                cancellationToken);

        if (attempt is null)
        {
            return AttemptNotFound;
        }

        if (!attempt.Good)
        {
            return NotGoodLift;
        }

        bool alreadyHasRecord = await dbContext.Set<Record>()
            .AnyAsync(r => r.AttemptId == attemptId, cancellationToken);

        if (alreadyHasRecord)
        {
            return AlreadyHasRecord;
        }

        Result<User> userResult = await dbContext.GetUserAsync(httpContextService, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.FromResult();

        Meet meet = attempt.Participation.Meet;
        DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);

        Era? era = await dbContext.Set<Era>()
            .Where(e => e.StartDate <= meetDate)
            .Where(e => e.EndDate >= meetDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (era is null)
        {
            return NoEraFound;
        }

        RecordCategory recordCategory = attempt.Discipline switch
        {
            Discipline.Squat => RecordCategory.Squat,
            Discipline.Bench => RecordCategory.Bench,
            Discipline.Deadlift => RecordCategory.Deadlift,
            _ => RecordCategory.None,
        };

        Record record = Record.Create(
            era.EraId,
            attempt.Participation.AgeCategoryId,
            attempt.Participation.WeightCategoryId,
            recordCategory,
            attempt.Weight,
            meetDate,
            attempt.AttemptId,
            meet.IsRaw,
            user.Username);

        dbContext.Set<Record>().Add(record);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}