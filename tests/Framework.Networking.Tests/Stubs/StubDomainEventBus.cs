using Proxyfan.Domain;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IDomainEventBus" /> that records all published events
///     for assertion in unit tests.
/// </summary>
public sealed class StubDomainEventBus : IDomainEventBus
{
    private readonly List<IDomainEvent> _published;

    /// <summary>
    ///     Gets all events that have been published to this bus, in order of publication.
    /// </summary>
    public IReadOnlyList<IDomainEvent> Published => _published;

    /// <summary>
    ///     Initializes a new instance of <see cref="StubDomainEventBus" />.
    /// </summary>
    public StubDomainEventBus()
    {
        List<IDomainEvent> published = [];
        _published = published;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        _published.Add(domainEvent);
    }

    /// <summary>
    ///     Returns all published events that are instances of <typeparamref name="TEvent" />.
    /// </summary>
    /// <typeparam name="TEvent">
    ///     The domain event type to filter by.
    /// </typeparam>
    /// <returns>
    ///     A sequence of matching published events.
    /// </returns>
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

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        var disposable = new NoOpDisposable();
        return disposable;
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}