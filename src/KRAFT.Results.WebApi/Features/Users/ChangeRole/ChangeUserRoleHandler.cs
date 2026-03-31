using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Roles;
using KRAFT.Results.WebApi.Features.UserRoles;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.ChangeRole;

internal sealed class ChangeUserRoleHandler
{
    private static readonly HashSet<string> AllowedRoles = ["Admin", "Editor", "User"];

    private readonly ILogger<ChangeUserRoleHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public ChangeUserRoleHandler(ILogger<ChangeUserRoleHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(int userId, IReadOnlyList<string> roleNames, CancellationToken cancellationToken)
    {
        if (roleNames.Count == 0)
        {
            return Result.Failure(UserErrors.RolesRequired);
        }

        if (roleNames.Any(roleName => !AllowedRoles.Contains(roleName)))
        {
            _logger.LogWarning("Attempted to assign disallowed role(s)");
            return Result.Failure(UserErrors.RoleNotFound);
        }

        Result<User> callerResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (callerResult.IsFailure)
        {
            return Result.Failure(callerResult.Error);
        }

        User caller = callerResult.FromResult();

        if (caller.UserId == userId)
        {
            _logger.LogWarning("User {Username} attempted to change their own role", caller.Username);
            return Result.Failure(UserErrors.CannotChangeOwnRole);
        }

        User? user = await _dbContext.Set<User>()
            .Include(u => u.UserRoles)
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("User with id '{UserId}' was not found", userId);
            return Result.Failure(UserErrors.UserNotFound);
        }

        List<Role> roles = await _dbContext.Set<Role>()
            .Where(r => roleNames.Contains(r.RoleName))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            return UserErrors.RoleNotFound;
        }

        HashSet<int> currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        HashSet<int> desiredRoleIds = roles.Select(r => r.RoleId).ToHashSet();

        List<UserRole> toRemove = user.UserRoles.Where(ur => !desiredRoleIds.Contains(ur.RoleId)).ToList();
        List<int> toAdd = desiredRoleIds.Where(id => !currentRoleIds.Contains(id)).ToList();

        _dbContext.Set<UserRole>().RemoveRange(toRemove);

        foreach (int roleId in toAdd)
        {
            _dbContext.Set<UserRole>().Add(UserRole.Create(user.UserId, roleId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}