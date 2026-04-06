namespace KRAFT.Results.WebApi.Abstractions;

internal interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

internal interface IDomainEventHandler<in T> : IDomainEventHandler
    where T : IDomainEvent
{
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken);
}