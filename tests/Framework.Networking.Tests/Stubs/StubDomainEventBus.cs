using Proxyfan.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IDomainEventBus" /> that records all published events
///     for assertion in unit tests.
/// </summary>
public sealed class StubDomainEventBus : IDomainEventBus
{
    private readonly List<IDomainEvent> _published;
    private readonly List<TaskCompletionSource<IDomainEvent>> _waitingTasks;

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
        var waiters = new List<TaskCompletionSource<IDomainEvent>>();
        _waitingTasks = waiters;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        lock (_published)
        {
            _published.Add(domainEvent);

            foreach (var waiter in _waitingTasks)
            {
                waiter.TrySetResult(domainEvent);
            }

            _waitingTasks.Clear();
        }
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

    /// <summary>
    ///     Waits asynchronously until an event of type <typeparamref name="TEvent" /> has been
    ///     published, or the cancellation token is cancelled.
    /// </summary>
    /// <typeparam name="TEvent">The event type to wait for.</typeparam>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    /// <returns>A task that completes when at least one matching event has been published.</returns>
    public async Task WaitForEventAsync<TEvent>(CancellationToken cancellationToken) where TEvent : IDomainEvent
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<IDomainEvent> waiter;

            lock (_published)
            {
                foreach (var e in _published)
                {
                    if (e is TEvent)
                    {
                        return;
                    }
                }

                waiter = new TaskCompletionSource<IDomainEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waitingTasks.Add(waiter);
            }

            using var registration = cancellationToken.Register(static state => ((TaskCompletionSource<IDomainEvent>)state!).TrySetCanceled(), waiter);
            var publishedEvent = await waiter.Task.ConfigureAwait(false);

            if (publishedEvent is TEvent)
            {
                return;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}