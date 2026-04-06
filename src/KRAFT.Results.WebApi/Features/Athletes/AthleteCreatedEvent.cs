using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed record class AthleteCreatedEvent(Athlete Athlete) : IDomainEvent;