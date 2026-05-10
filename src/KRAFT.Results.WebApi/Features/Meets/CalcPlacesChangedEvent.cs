using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed record class CalcPlacesChangedEvent(string Slug, bool CalcPlaces) : IDomainEvent;