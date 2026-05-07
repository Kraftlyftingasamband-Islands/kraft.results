using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Participations.ComputePlaces;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.UpdateBodyWeight;

internal sealed class UpdateBodyWeightHandler
{
    private readonly ILogger<UpdateBodyWeightHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;
    private readonly PlaceComputationService _placeComputationService;

    public UpdateBodyWeightHandler(
        ILogger<UpdateBodyWeightHandler> logger,
        ResultsDbContext dbContext,
        IHttpContextService httpContextService,
        PlaceComputationService placeComputationService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
        _placeComputationService = placeComputationService;
    }

    public async Task<Result> Handle(
        int meetId,
        int participationId,
        UpdateBodyWeightCommand command,
        CancellationToken cancellationToken)
    {
        Participation? participation = await _dbContext.Set<Participation>()
            .Include(p => p.Meet)
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

        Result updateResult = participation.UpdateBodyWeight(command.BodyWeight, user.Username);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _placeComputationService.ComputePlacesAsync(participation, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated body weight for participation {ParticipationId} in meet {MeetId} to {BodyWeight}",
            participationId,
            meetId,
            command.BodyWeight);

        return Result.Success();
    }
}