using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;

namespace KRAFT.Results.WebApi.Features.Records.ComputeRecords;

#pragma warning disable CA1031 // ADR-0001: record computation failure must not fail the attempt save
internal sealed class AttemptRecordedEventHandler(
    RecordComputationService recordComputationService,
    ILogger<AttemptRecordedEventHandler> logger) : IDomainEventHandler<AttemptRecordedEvent>
{
    private readonly RecordComputationService _recordComputationService = recordComputationService;
    private readonly ILogger<AttemptRecordedEventHandler> _logger = logger;

    public async Task HandleAsync(AttemptRecordedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            Participation participation = domainEvent.Participation;

            Attempt? attempt = participation.Attempts
                .OrderByDescending(a => a.AttemptId)
                .FirstOrDefault();

            if (attempt is null)
            {
                _logger.LogWarning(
                    "AttemptRecordedEvent fired for participation {ParticipationId} but no attempts found",
                    participation.ParticipationId);
                return;
            }

            _logger.LogInformation(
                "Computing records for attempt {AttemptId} on participation {ParticipationId}",
                attempt.AttemptId,
                participation.ParticipationId);

            await _recordComputationService.ComputeRecordsAsync(attempt.AttemptId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to compute records for participation {ParticipationId}",
                domainEvent.Participation.ParticipationId);
        }
    }

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is AttemptRecordedEvent attemptRecordedEvent)
        {
            return HandleAsync(attemptRecordedEvent, cancellationToken);
        }

        return Task.CompletedTask;
    }
}