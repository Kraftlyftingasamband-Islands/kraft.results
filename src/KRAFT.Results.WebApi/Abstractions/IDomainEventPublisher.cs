namespace KRAFT.Results.WebApi.Abstractions;

internal interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}