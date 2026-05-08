using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Participations.ComputePlaces;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.RecordAttempt;

internal sealed class RecordAttemptHandler
{
    private const short MinRound = 1;
    private const short MaxRound = 3;

    private readonly ILogger<RecordAttemptHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;
    private readonly PlaceComputationService _placeComputationService;

    public RecordAttemptHandler(
        ILogger<RecordAttemptHandler> logger,
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
        Discipline discipline,
        short round,
        RecordAttemptCommand command,
        CancellationToken cancellationToken)
    {
        Result validationResult = Validate(discipline, round, command);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Participation? participation = await _dbContext.Set<Participation>()
            .Include(p => p.Attempts)
            .Include(p => p.Meet)
            .Include(p => p.Athlete)
            .ThenInclude(a => a.Bans)
            .Where(p => p.ParticipationId == participationId)
            .FirstOrDefaultAsync(
                p => p.MeetId == meetId,
                cancellationToken);

        if (participation is null)
        {
            _logger.LogWarning(
                "Participation {ParticipationId} not found for meet {MeetId}",
                participationId,
                meetId);
            return MeetErrors.ParticipationNotFound;
        }

        foreach (Attempt existingAttempt in participation.Attempts
            .Where(a => a.Discipline == discipline)
            .Where(a => a.Round >= MinRound)
            .Where(a => a.Round <= MaxRound)
            .Where(a => a.Weight > 0))
        {
            if (existingAttempt.Round < round && existingAttempt.Weight > command.Weight)
            {
                return MeetErrors.AttemptOutOfOrder;
            }

            if (existingAttempt.Round > round && existingAttempt.Weight < command.Weight)
            {
                return MeetErrors.AttemptOutOfOrder;
            }
        }

        Result<User> userResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.FromResult();

        Attempt? existing = participation.Attempts
            .Where(a => a.Discipline == discipline)
            .FirstOrDefault(a => a.Round == round);

        if (existing is not null)
        {
            participation.UpdateAttempt(existing, command.Weight, command.Good, user.Username);
        }
        else
        {
            participation.RecordAttempt(discipline, round, command.Weight, command.Good, user.Username);
        }

        participation.RecalculateTotals();

        await _placeComputationService.ComputePlacesAsync(participation, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded attempt for participation {ParticipationId} in meet {MeetId}: {Discipline} round {Round}",
            participationId,
            meetId,
            discipline,
            round);

        return Result.Success();
    }

    private static Result Validate(Discipline discipline, short round, RecordAttemptCommand command)
    {
        if (discipline == Discipline.None || !Enum.IsDefined(discipline))
        {
            return new Error(
                "Meets.InvalidDiscipline",
                "Discipline is not valid. Must be Squat, Bench, or Deadlift.");
        }

        if (round < MinRound || round > MaxRound)
        {
            return new Error(
                "Meets.InvalidRound",
                $"Round {round} is not valid. Must be 1, 2, or 3.");
        }

        if (command.Weight <= 0)
        {
            return new Error(
                "Meets.InvalidWeight",
                "Weight must be greater than 0.");
        }

        return Result.Success();
    }
}