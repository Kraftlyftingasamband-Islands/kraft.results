using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.UpdateBodyWeight;

internal sealed class UpdateBodyWeightHandler
{
    private readonly ILogger<UpdateBodyWeightHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateBodyWeightHandler(
        ILogger<UpdateBodyWeightHandler> logger,
        ResultsDbContext dbContext,
        IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(
        int meetId,
        int participationId,
        UpdateBodyWeightCommand command,
        CancellationToken cancellationToken)
    {
        if (command.BodyWeight <= 0)
        {
            return MeetErrors.InvalidBodyWeight;
        }

        Participation? participation = await _dbContext.Set<Participation>()
            .FirstOrDefaultAsync(
                p => p.ParticipationId == participationId && p.MeetId == meetId,
                cancellationToken);

        if (participation is null)
        {
            _logger.LogWarning(
                "Participation {ParticipationId} not found for meet {MeetId}",
                participationId,
                meetId);
            return MeetErrors.ParticipationNotFound;
        }

        Result<User> userResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.FromResult();

        participation.UpdateBodyWeight(command.BodyWeight, user.Username);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated body weight for participation {ParticipationId} in meet {MeetId} to {BodyWeight}",
            participationId,
            meetId,
            command.BodyWeight);

        return Result.Success();
    }
}