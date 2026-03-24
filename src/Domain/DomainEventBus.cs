using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain;

/// <summary>
///     In-process domain event bus backed by a <see cref="ConcurrentDictionary{TKey,TValue}" />
///     of handler lists. Delivery is synchronous on the calling thread.
/// </summary>
public sealed class DomainEventBus : IDomainEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly Lock _handlersLock = new();

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        var key = typeof(TEvent);

        lock (_handlersLock)
        {
            var list = _handlers.GetOrAdd(key, _ => []);
            list.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_handlersLock)
            {
                if (_handlers.TryGetValue(key, out var list))
                {
                    list.Remove(handler);
                }
            }
        });
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        var key = typeof(TEvent);

        Delegate[]? snapshot;

        lock (_handlersLock)
        {
            if (!_handlers.TryGetValue(key, out var list))
            {
                return;
            }

            snapshot =
            [
                ..list,
            ];
        }

        foreach (var handler in snapshot)
        {
            try
            {
                ((Action<TEvent>)handler)(domainEvent);
            }
            catch
            {
                // Handler exceptions are swallowed so remaining handlers still execute.
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            unsubscribe();
        }
    }
}
