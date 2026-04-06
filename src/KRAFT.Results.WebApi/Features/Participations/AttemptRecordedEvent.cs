using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Participations;

internal sealed record class AttemptRecordedEvent(Participation Participation) : IDomainEvent;