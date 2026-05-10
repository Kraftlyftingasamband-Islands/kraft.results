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
            "Processing CalcPlacesChangedEvent for meet {Slug}, CalcPlaces={CalcPlaces}",
            domainEvent.Slug,
            domainEvent.CalcPlaces);

        await _placeComputationService.RecomputeMeetAsync(domainEvent.Slug, domainEvent.CalcPlaces, cancellationToken);
    }

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return HandleAsync((CalcPlacesChangedEvent)domainEvent, cancellationToken);
    }
}