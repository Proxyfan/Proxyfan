namespace Proxyfan.Domain;

/// <summary>
///     Represents a handler for a domain event of type <typeparamref name="TEvent" />.
/// </summary>
/// <typeparam name="TEvent">The domain event type to handle.</typeparam>
/// <param name="domainEvent">The domain event instance to process.</param>
public delegate void DomainEventHandler<in TEvent>(TEvent domainEvent)
    where TEvent : IDomainEvent;