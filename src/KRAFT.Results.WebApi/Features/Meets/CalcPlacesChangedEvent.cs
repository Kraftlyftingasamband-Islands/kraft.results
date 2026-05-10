using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed record class CalcPlacesChangedEvent(int MeetId, bool CalcPlaces) : IDomainEvent;