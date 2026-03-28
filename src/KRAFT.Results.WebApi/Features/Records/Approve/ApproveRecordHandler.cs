using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records.Approve;

internal sealed class ApproveRecordHandler(
    ResultsDbContext dbContext,
    IHttpContextService httpContextService)
{
    public async Task<Result> Handle(int recordId, CancellationToken cancellationToken)
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

        Result approveResult = record.Approve(user.Username);

        if (approveResult.IsFailure)
        {
            return approveResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}