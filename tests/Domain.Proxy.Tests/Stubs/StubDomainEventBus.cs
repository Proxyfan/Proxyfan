using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IDomainEventBus" /> that records all published events
///     for assertion in unit tests and supports awaiting future events via
///     <see cref="WaitForPublishAsync{TEvent}" /> and <see cref="WaitForNextPublishAsync{TEvent}" />.
/// </summary>
public sealed class StubDomainEventBus : IDomainEventBus
{
    private readonly List<IDomainEvent> _published;
    private readonly Dictionary<Type, Queue<TaskCompletionSource<IDomainEvent>>> _waiters;

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
        Dictionary<Type, Queue<TaskCompletionSource<IDomainEvent>>> waiters = new();
        _waiters = waiters;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        _published.Add(domainEvent);

        var key = typeof(TEvent);

        if (!_waiters.TryGetValue(key, out var queue) || queue.Count == 0)
        {
            return;
        }

        var tcs = queue.Dequeue();
        tcs.TrySetResult(domainEvent);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        var disposable = new NoOpDisposable();
        return disposable;
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

    /// <summary>
    ///     Returns a task that completes with the first already-published event of type
    ///     <typeparamref name="TEvent" />, or waits for the next one if none have been published yet.
    ///     Use <see cref="WaitForNextPublishAsync{TEvent}" /> to always wait for the next future event.
    /// </summary>
    /// <typeparam name="TEvent">
    ///     The domain event type to wait for.
    /// </typeparam>
    /// <param name="cancellationToken">
    ///     A cancellation token that cancels the wait.
    /// </param>
    /// <returns>
    ///     A task resolving to the first matching published event.
    /// </returns>
    public async Task<TEvent> WaitForPublishAsync<TEvent>(CancellationToken cancellationToken) where TEvent : IDomainEvent
    {
        foreach (var e in _published)
        {
            if (e is TEvent typed)
            {
                return typed;
            }
        }

        return await WaitForNextPublishAsync<TEvent>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns a task that completes when the next event of type <typeparamref name="TEvent" />
    ///     is published, regardless of any previously published events.
    /// </summary>
    /// <typeparam name="TEvent">
    ///     The domain event type to wait for.
    /// </typeparam>
    /// <param name="cancellationToken">
    ///     A cancellation token that cancels the wait.
    /// </param>
    /// <returns>
    ///     A task resolving to the next published event of type <typeparamref name="TEvent" />.
    /// </returns>
    public async Task<TEvent> WaitForNextPublishAsync<TEvent>(CancellationToken cancellationToken) where TEvent : IDomainEvent
    {
        var key = typeof(TEvent);

        if (!_waiters.TryGetValue(key, out var queue))
        {
            queue = new Queue<TaskCompletionSource<IDomainEvent>>();
            _waiters[key] = queue;
        }

        var tcs = new TaskCompletionSource<IDomainEvent>();
        queue.Enqueue(tcs);

        var published = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        if (published is TEvent result)
        {
            return result;
        }

        throw new InvalidOperationException("Published event was not of the expected type.");
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}