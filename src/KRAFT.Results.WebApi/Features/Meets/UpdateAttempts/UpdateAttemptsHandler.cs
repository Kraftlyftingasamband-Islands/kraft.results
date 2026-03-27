using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.UpdateAttempts;

internal sealed class UpdateAttemptsHandler
{
    private const int MaxAttempts = 9;
    private const short MinRound = 1;
    private const short MaxRound = 3;

    private readonly ILogger<UpdateAttemptsHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateAttemptsHandler(
        ILogger<UpdateAttemptsHandler> logger,
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
        UpdateAttemptsCommand command,
        CancellationToken cancellationToken)
    {
        Result validationResult = Validate(command);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Participation? participation = await _dbContext.Set<Participation>()
            .Include(p => p.Attempts)
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

        User user = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        List<int> existingAttemptIds = participation.Attempts
            .Select(a => a.AttemptId)
            .ToList();

        if (existingAttemptIds.Count > 0)
        {
            await _dbContext.Set<Record>()
                .Where(r => r.AttemptId != null && existingAttemptIds.Contains(r.AttemptId.Value))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.AttemptId, (int?)null),
                    cancellationToken);
        }

        _dbContext.Set<Attempt>().RemoveRange(participation.Attempts);
        participation.Attempts.Clear();

        foreach (AttemptEntry entry in command.Attempts)
        {
            Attempt attempt = Attempt.Create(
                participationId,
                (byte)entry.Discipline,
                entry.Round,
                entry.Weight,
                entry.Good,
                user.Username);

            _dbContext.Set<Attempt>().Add(attempt);
            participation.Attempts.Add(attempt);
        }

        participation.RecalculateTotals();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated {AttemptCount} attempts for participation {ParticipationId} in meet {MeetId}",
            command.Attempts.Count,
            participationId,
            meetId);

        return Result.Success();
    }

    private static Result Validate(UpdateAttemptsCommand command)
    {
        if (command.Attempts.Count > MaxAttempts)
        {
            return new Error("Meets.TooManyAttempts", $"Cannot exceed {MaxAttempts} attempts.");
        }

        HashSet<(Discipline Discipline, short Round)> seen = [];

        foreach (AttemptEntry entry in command.Attempts)
        {
            if (entry.Discipline == Discipline.None || !Enum.IsDefined(entry.Discipline))
            {
                return new Error(
                    "Meets.InvalidDiscipline",
                    "Discipline is not valid. Must be Squat, Bench, or Deadlift.");
            }

            if (entry.Round < MinRound || entry.Round > MaxRound)
            {
                return new Error(
                    "Meets.InvalidRound",
                    $"Round {entry.Round} is not valid. Must be 1, 2, or 3.");
            }

            if (entry.Weight <= 0)
            {
                return new Error(
                    "Meets.InvalidWeight",
                    "Weight must be greater than 0.");
            }

            if (!seen.Add((entry.Discipline, entry.Round)))
            {
                return new Error(
                    "Meets.DuplicateAttempt",
                    $"Duplicate attempt for {entry.Discipline} round {entry.Round}.");
            }
        }

        return Result.Success();
    }
}