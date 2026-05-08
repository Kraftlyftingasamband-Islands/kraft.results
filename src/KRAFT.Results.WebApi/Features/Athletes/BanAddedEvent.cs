using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed record class BanAddedEvent(int AthleteId, DateTime FromDate, DateTime ToDate) : IDomainEvent;