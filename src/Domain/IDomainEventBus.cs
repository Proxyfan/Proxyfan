using System;

namespace Proxyfan.Domain;

/// <summary>
///     Defines an in-process domain event bus for publishing and subscribing to domain events
///     across bounded contexts.
/// </summary>
/// <remarks>
///     Delivery is synchronous and fire-and-forget: <see cref="Publish{TEvent}" /> calls all
///     registered handlers inline on the calling thread. Handler exceptions are caught and do
///     not prevent subsequent handlers from executing.
/// </remarks>
public interface IDomainEventBus
{
    /// <summary>
    ///     Registers a handler to be invoked whenever an event of type
    ///     <typeparamref name="TEvent" /> is published.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type to subscribe to.</typeparam>
    /// <param name="handler">The handler invoked when the event is published.</param>
    /// <returns>
    ///     An <see cref="IDisposable" /> that, when disposed, removes the handler registration.
    /// </returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent;

    /// <summary>
    ///     Publishes a domain event, synchronously invoking all registered handlers for
    ///     <typeparamref name="TEvent" />.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type to publish.</typeparam>
    /// <param name="domainEvent">The event instance to deliver to subscribers.</param>
    void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
}
