using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records.DetectRecords;
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
    private readonly RecordDetectionService _recordDetectionService;

    public RecordAttemptHandler(
        ILogger<RecordAttemptHandler> logger,
        ResultsDbContext dbContext,
        IHttpContextService httpContextService,
        RecordDetectionService recordDetectionService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
        _recordDetectionService = recordDetectionService;
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

        Attempt? existing = participation.Attempts
            .FirstOrDefault(a => a.Discipline == discipline && a.Round == round);

        Attempt savedAttempt;

        if (existing is not null)
        {
            existing.Update(command.Weight, command.Good, user.Username);
            savedAttempt = existing;
        }
        else
        {
            Attempt attempt = Attempt.Create(
                participationId,
                discipline,
                round,
                command.Weight,
                command.Good,
                user.Username);

            _dbContext.Set<Attempt>().Add(attempt);
            participation.Attempts.Add(attempt);
            savedAttempt = attempt;
        }

        participation.RecalculateTotals();

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _recordDetectionService.DetectAsync(
            participation,
            savedAttempt,
            participation.Meet,
            user.Username,
            cancellationToken);

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