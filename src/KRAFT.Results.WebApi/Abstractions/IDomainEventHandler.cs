namespace KRAFT.Results.WebApi.Abstractions;

internal interface IDomainEventHandler<in T>
    where T : IDomainEvent
{
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken);
}