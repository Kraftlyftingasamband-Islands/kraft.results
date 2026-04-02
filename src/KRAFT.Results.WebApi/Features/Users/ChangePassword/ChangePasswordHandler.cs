using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.ChangePassword;

internal sealed class ChangePasswordHandler
{
    private readonly ILogger<ChangePasswordHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public ChangePasswordHandler(ILogger<ChangePasswordHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        Result<User> userResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.FromResult();

        Result result = user.ChangePassword(command.CurrentPassword, command.NewPassword, command.ConfirmNewPassword);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to change password for user '{Username}': {ErrorCode}", user.Username, result.Error.Code);
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed successfully for user '{Username}'", user.Username);

        return Result.Success();
    }
}