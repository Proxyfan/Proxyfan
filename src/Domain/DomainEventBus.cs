using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain;

/// <summary>
///     In-process domain event bus backed by a typed handler collection per event type.
///     Delivery is synchronous on the calling thread. Handler exceptions are logged and
///     swallowed so that one failing handler does not prevent subsequent handlers from running.
/// </summary>
public sealed class DomainEventBus : IDomainEventBus
{
    private readonly ConcurrentDictionary<Type, IHandlerCollection> _handlers;
    private readonly Lock _handlersLock;
    private readonly ILogger<DomainEventBus> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="DomainEventBus" />.
    /// </summary>
    /// <param name="logger">Logger used to record swallowed handler exceptions.</param>
    public DomainEventBus(ILogger<DomainEventBus> logger)
    {
        var handlers = new ConcurrentDictionary<Type, IHandlerCollection>();
        _handlers = handlers;
        var handlersLock = new Lock();
        _handlersLock = handlersLock;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        var key = typeof(TEvent);
        IHandlerCollection? collection;

        lock (_handlersLock)
        {
            if (!_handlers.TryGetValue(key, out collection))
            {
                return;
            }
        }

        if (collection is HandlerCollection<TEvent> typedCollection)
        {
            typedCollection.InvokeAll(domainEvent, _logger);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        var key = typeof(TEvent);

        lock (_handlersLock)
        {
            var existing = _handlers.GetOrAdd(key, CreateCollection);

            if (existing is HandlerCollection<TEvent> typedCollection)
            {
                typedCollection.Add(handler);
            }
        }

        var subscription = new Subscription(Unsubscribe);
        return subscription;

        IHandlerCollection CreateCollection(Type _)
        {
            var collection = new HandlerCollection<TEvent>();
            return collection;
        }

        void Unsubscribe()
        {
            lock (_handlersLock)
            {
                if (_handlers.TryGetValue(key, out var existing) &&
                    existing is HandlerCollection<TEvent> typedCollection)
                {
                    typedCollection.Remove(handler);
                }
            }
        }
    }

    private sealed class HandlerCollection<TEvent> : IHandlerCollection
        where TEvent : IDomainEvent
    {
        private readonly List<DomainEventHandler<TEvent>> _items;

        public HandlerCollection()
        {
            List<DomainEventHandler<TEvent>> items = [];
            _items = items;
        }

        public void Add(DomainEventHandler<TEvent> handler)
        {
            _items.Add(handler);
        }

        public void InvokeAll(TEvent domainEvent, ILogger logger)
        {
            var snapshot = _items.ToArray();

            foreach (var handler in snapshot)
            {
                try
                {
                    handler(domainEvent);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "A domain event handler threw an unhandled exception for event type {EventType}.",
                        typeof(TEvent).Name);
                }
            }
        }

        public void Remove(DomainEventHandler<TEvent> handler)
        {
            _items.Remove(handler);
        }
    }

    private interface IHandlerCollection
    {
    }

    private sealed class Subscription : IDisposable
    {
        private readonly UnsubscribeHandler _unsubscribe;
        private bool _isDisposed;

        public Subscription(UnsubscribeHandler unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _unsubscribe();
        }
    }

    private delegate void UnsubscribeHandler();
}