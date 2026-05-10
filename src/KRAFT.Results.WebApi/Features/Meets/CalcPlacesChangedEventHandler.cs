using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations.ComputePlaces;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class CalcPlacesChangedEventHandler(
    PlaceComputationService placeComputationService,
    ILogger<CalcPlacesChangedEventHandler> logger) : IDomainEventHandler<CalcPlacesChangedEvent>
{
    private readonly PlaceComputationService _placeComputationService = placeComputationService;
    private readonly ILogger<CalcPlacesChangedEventHandler> _logger = logger;

    public async Task HandleAsync(CalcPlacesChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing CalcPlacesChangedEvent for meet {MeetId}, CalcPlaces={CalcPlaces}",
            domainEvent.MeetId,
            domainEvent.CalcPlaces);

        await _placeComputationService.RecomputeMeetAsync(domainEvent.MeetId, domainEvent.CalcPlaces, cancellationToken);
    }

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is CalcPlacesChangedEvent calcPlacesChangedEvent)
        {
            return HandleAsync(calcPlacesChangedEvent, cancellationToken);
        }

        return Task.CompletedTask;
    }
}