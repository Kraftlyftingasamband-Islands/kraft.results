using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Participations;

internal sealed record class ParticipationAddedEvent(Participation Participation) : IDomainEvent;