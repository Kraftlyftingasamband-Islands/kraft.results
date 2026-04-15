using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;

namespace KRAFT.Results.WebApi.Features.Records.ComputeRecords;

internal sealed class AttemptRecordedEventHandler(
    RecordComputationChannel channel,
    ILogger<AttemptRecordedEventHandler> logger) : IDomainEventHandler<AttemptRecordedEvent>
{
    private readonly RecordComputationChannel _channel = channel;
    private readonly ILogger<AttemptRecordedEventHandler> _logger = logger;

    public async Task HandleAsync(AttemptRecordedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Enqueuing record computation for attempt {AttemptId} on participation {ParticipationId}",
            domainEvent.Attempt.AttemptId,
            domainEvent.Participation.ParticipationId);

        await _channel.WriteAsync(domainEvent.Attempt.AttemptId, cancellationToken);
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