using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users.Infrastructure;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Users.Login;

internal sealed class LoginHandler
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly TokenProvider _tokenProvider;

    public LoginHandler(ILogger<LoginHandler> logger, ResultsDbContext dbContext, TokenProvider tokenProvider)
    {
        _logger = logger;
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
    }

    public async Task<Result<AuthenticatedResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
        {
            return UserErrors.InvalidUsernameOrPassword;
        }

        if (await _dbContext.Set<User>()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == command.Username, cancellationToken) is not User user)
        {
            _logger.LogWarning("Login attempt with invalid username: {Username}", command.Username);
            return UserErrors.InvalidUsernameOrPassword;
        }

        if (!user.Password.IsHashed)
        {
            _logger.LogInformation("Migrating password hash for user: {Username}", command.Username);
            user.Password = Password.Hash(user.Password);
        }

        if (!user.Password.Verify(command.Password))
        {
            _logger.LogWarning("Login attempt with invalid password for username: {Username}", command.Username);
            return UserErrors.InvalidUsernameOrPassword;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        string token = _tokenProvider.CreateToken(user);

        return new AuthenticatedResponse(token, string.Empty);
    }
}