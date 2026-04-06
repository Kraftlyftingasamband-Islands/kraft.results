# 0001. Domain Events Infrastructure

**Date:** 2026-04-06
**Status:** Accepted

## Context

The codebase has aggregate roots with factory methods and behaviour methods, but no mechanism to signal that a meaningful domain state change occurred. Side effects (notifications, read model updates, audit trails) must eventually be decoupled from the primary handler. MediatR is not used in this project.

Events must only be dispatched after successful persistence — dispatching before save risks acting on uncommitted state.

## Decision

Introduce a custom in-process domain event system:

- `IDomainEvent` marker interface in `Abstractions/`
- `IDomainEventHandler<T>` interface for future consumers
- `AggregateRoot` abstract base class with event collection (`Raise`, `DomainEvents`, `ClearDomainEvents`)
- All aggregate roots inherit `AggregateRoot`
- `IDomainEventPublisher` / `DomainEventPublisher` — resolves `IDomainEventHandler<T>` instances from DI and invokes them via reflection
- `DomainEventInterceptor` (EF Core `SaveChangesInterceptor`) — dispatches collected events from all tracked `AggregateRoot` instances after `SaveChangesAsync` succeeds
- Event records hold a reference to the aggregate instance (not scalar IDs) so that EF-assigned identity values are available to handlers at dispatch time

## Consequences

- Aggregate roots can raise events without knowing about handlers; handlers are registered independently via DI.
- If a handler throws, the exception propagates to the caller. The database write is already committed and will not roll back. Handlers are responsible for their own resilience.
- No at-least-once delivery guarantee. If the process crashes between save and dispatch, events are lost. An outbox pattern can be layered on later if needed.
- The interceptor scans all tracked `AggregateRoot` instances regardless of `EntityState` to handle the case where an aggregate raises an event without mutating an EF-tracked property.
