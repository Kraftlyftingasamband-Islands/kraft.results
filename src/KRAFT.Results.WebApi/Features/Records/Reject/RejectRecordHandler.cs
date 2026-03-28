using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records.Reject;

internal sealed class RejectRecordHandler(
    ResultsDbContext dbContext,
    IHttpContextService httpContextService)
{
    public async Task<Result> Handle(int recordId, string? reason, CancellationToken cancellationToken)
    {
        Record? record = await dbContext.Set<Record>()
            .FirstOrDefaultAsync(r => r.RecordId == recordId, cancellationToken);

        if (record is null)
        {
            return RecordErrors.RecordNotFound;
        }

        Result<User> userResult = await dbContext.GetUserAsync(httpContextService, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.FromResult();

        Result rejectResult = record.Reject(reason, user.Username);

        if (rejectResult.IsFailure)
        {
            return rejectResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}