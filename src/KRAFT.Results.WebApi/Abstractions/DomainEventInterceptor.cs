using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KRAFT.Results.WebApi.Abstractions;

internal sealed class DomainEventInterceptor(IDomainEventPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        List<IDomainEvent> domainEvents = eventData.Context.ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AggregateRoot> entry in
            eventData.Context.ChangeTracker.Entries<AggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await publisher.PublishAsync(domainEvent, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}