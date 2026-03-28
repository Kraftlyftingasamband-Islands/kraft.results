using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserDbContextExtensions
{
    internal static async Task<Result<User>> GetUserAsync(this DbContext dbContext, IHttpContextService httpContextService, CancellationToken cancellationToken)
    {
        string? userName = httpContextService.GetUserName();

        if (userName is null)
        {
            return UserErrors.UserNameClaimMissing;
        }

        User? user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(x => x.Username == userName, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNameClaimMissing;
        }

        return user;
    }
}