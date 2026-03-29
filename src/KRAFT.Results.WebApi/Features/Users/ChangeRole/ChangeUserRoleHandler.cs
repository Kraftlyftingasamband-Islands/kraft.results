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

    public async Task<Result> Handle(int userId, string roleName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Result.Failure(UserErrors.RoleNotFound);
        }

        if (!AllowedRoles.Contains(roleName))
        {
            _logger.LogWarning("Attempted to assign disallowed role '{RoleName}'", roleName);
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

        Role? role = await _dbContext.Set<Role>()
            .Where(r => r.RoleName == roleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            _logger.LogWarning("Role '{RoleName}' was not found", roleName);
            return Result.Failure(UserErrors.RoleNotFound);
        }

        _dbContext.Set<UserRole>().RemoveRange(user.UserRoles);
        _dbContext.Set<UserRole>().Add(UserRole.Create(user.UserId, role.RoleId));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}