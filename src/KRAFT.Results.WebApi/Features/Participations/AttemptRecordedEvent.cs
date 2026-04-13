using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Attempts;

namespace KRAFT.Results.WebApi.Features.Participations;

internal sealed record class AttemptRecordedEvent(Participation Participation, Attempt Attempt) : IDomainEvent;