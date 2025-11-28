using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.Create;

internal sealed class CreateUserHandler
{
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public CreateUserHandler(ILogger<CreateUserHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result<int>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(command.Email);

        if (email.IsFailure)
        {
            return email.Error;
        }

        Result<User> user = User.Create(
            userName: command.UserName,
            firstName: command.FirstName,
            lastName: command.LastName,
            email: email,
            password: command.Password);

        if (user.IsFailure)
        {
            return user.Error;
        }

        if (await _dbContext.Set<User>().AnyAsync(x => x.Username == command.UserName, cancellationToken))
        {
            _logger.LogWarning("Username {Username} already exists", command.UserName);
            return UserErrors.UserNameExists;
        }

        if (await _dbContext.Set<User>().AnyAsync(x => x.Email == email.FromResult(), cancellationToken))
        {
            _logger.LogWarning("Username {Username} already exists", command.UserName);
            return UserErrors.UserNameExists;
        }

        _dbContext.Set<User>().Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Username} created", command.UserName);

        return user.FromResult().UserId;
    }
}