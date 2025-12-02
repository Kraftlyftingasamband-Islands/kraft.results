using KRAFT.Results.Contracts.Users;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.Get;

internal sealed class GetUsersHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetUsersHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<UserSummary>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<User>()
        .Select(x => new UserSummary(
            $"{x.Firstname} {x.Lastname}",
            x.Email,
            x.CreatedOn,
            x.UserRoles.Select(x => x.Role.RoleName).Order().ToList()))
        .ToListAsync(cancellationToken);
}