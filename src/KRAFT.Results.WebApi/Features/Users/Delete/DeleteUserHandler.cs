using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.Delete;

internal sealed class DeleteUserHandler
{
    private readonly ILogger<DeleteUserHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public DeleteUserHandler(ILogger<DeleteUserHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(int userId, CancellationToken cancellationToken)
    {
        Result<User> callerResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (callerResult.IsFailure)
        {
            return Result.Failure(callerResult.Error);
        }

        User caller = callerResult.FromResult();

        if (caller.UserId == userId)
        {
            _logger.LogWarning("User {Username} attempted to delete their own account", caller.Username);
            return Result.Failure(UserErrors.CannotDeleteSelf);
        }

        User? user = await _dbContext.Set<User>()
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("User with id '{UserId}' was not found", userId);
            return Result.Failure(UserErrors.UserNotFound);
        }

        _dbContext.Set<User>().Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}