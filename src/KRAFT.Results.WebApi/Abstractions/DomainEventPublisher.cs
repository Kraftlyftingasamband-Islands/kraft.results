using Microsoft.Extensions.DependencyInjection;

namespace KRAFT.Results.WebApi.Abstractions;

internal sealed class DomainEventPublisher(IServiceProvider serviceProvider) : IDomainEventPublisher
{
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Type eventType = domainEvent.GetType();
        Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        IEnumerable<object?> handlers = serviceProvider.GetServices(handlerType);

        foreach (object? handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            System.Reflection.MethodInfo? method = handlerType.GetMethod("HandleAsync");
            if (method is not null)
            {
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}