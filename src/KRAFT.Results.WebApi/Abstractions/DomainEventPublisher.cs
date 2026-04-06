using Microsoft.Extensions.DependencyInjection;

namespace KRAFT.Results.WebApi.Abstractions;

internal sealed class DomainEventPublisher(IServiceScopeFactory serviceScopeFactory) : IDomainEventPublisher
{
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Type eventType = domainEvent.GetType();
        Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (object? handler in handlers)
        {
            if (handler is IDomainEventHandler typedHandler)
            {
                await typedHandler.HandleAsync(domainEvent, cancellationToken);
            }
        }
    }
}