using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Services;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.Update;

internal sealed class UpdateUserHandler
{
    private readonly ILogger<UpdateUserHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateUserHandler(ILogger<UpdateUserHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(int userId, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        Result<User> modifierResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (modifierResult.IsFailure)
        {
            return Result.Failure(modifierResult.Error);
        }

        User modifier = modifierResult.FromResult();

        User? user = await _dbContext.Set<User>()
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("User with id '{UserId}' was not found", userId);
            return Result.Failure(UserErrors.UserNotFound);
        }

        Result<Email> email = Email.Create(command.Email);

        if (email.IsFailure)
        {
            return Result.Failure(email.Error);
        }

        bool emailTakenByOther = await _dbContext.Set<User>()
            .AnyAsync(x => x.Email == email.FromResult() && x.UserId != userId, cancellationToken);

        if (emailTakenByOther)
        {
            _logger.LogWarning("Failed to update user {UserId}: Email {Email} is already in use", userId, command.Email);
            return Result.Failure(UserErrors.EmailExists);
        }

        Result result = user.Update(
            modifier: modifier,
            firstName: command.FirstName,
            lastName: command.LastName,
            email: email);

        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}