using KRAFT.Results.Contracts.Users;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.GetEditDetails;

internal sealed class GetUserEditDetailsHandler(ResultsDbContext dbContext)
{
    public Task<UserEditDetails?> Handle(int userId, CancellationToken cancellationToken) =>
        dbContext.Set<User>()
            .Where(x => x.UserId == userId)
            .Select(x => new UserEditDetails(
                x.Firstname!,
                x.Lastname!,
                x.Email,
                x.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? string.Empty))
            .FirstOrDefaultAsync(cancellationToken);
}