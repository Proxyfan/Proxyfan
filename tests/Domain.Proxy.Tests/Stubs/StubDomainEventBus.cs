using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IDomainEventBus" /> that records published events
///     for assertion in unit tests.
/// </summary>
internal sealed class StubDomainEventBus : IDomainEventBus
{
    private readonly List<IDomainEvent> _published = [];

    /// <summary>Gets all events that have been published to this bus.</summary>
    public IReadOnlyList<IDomainEvent> Published => _published;

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        return new NoOpDisposable();
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        _published.Add(domainEvent);
    }

    /// <summary>Returns all published events of type <typeparamref name="TEvent" />.</summary>
    public IEnumerable<TEvent> PublishedOf<TEvent>() where TEvent : IDomainEvent
    {
        foreach (var e in _published)
        {
            if (e is TEvent typed)
            {
                yield return typed;
            }
        }
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
