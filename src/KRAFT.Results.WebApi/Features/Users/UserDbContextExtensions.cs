using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserDbContextExtensions
{
    internal static Task<User> GetUserAsync(this DbContext dbContext, IHttpContextService httpContextService, CancellationToken cancellationToken) =>
        dbContext.Set<User>()
        .FirstAsync(x => x.Username == httpContextService.GetUserName(), cancellationToken);
}