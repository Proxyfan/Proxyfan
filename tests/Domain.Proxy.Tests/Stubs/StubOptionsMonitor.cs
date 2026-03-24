using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IOptionsMonitor{TOptions}" /> that supports
///     programmatically triggering change notifications for testing hot-reload scenarios.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
internal sealed class StubOptionsMonitor<T>(T initialValue) : IOptionsMonitor<T>
{
    private readonly List<Action<T, string?>> _listeners = [];

    /// <inheritdoc />
    public T CurrentValue { get; private set; } = initialValue;

    /// <inheritdoc />
    public T Get(string? name)
    {
        return CurrentValue;
    }

    /// <inheritdoc />
    public IDisposable? OnChange(Action<T, string?> listener)
    {
        _listeners.Add(listener);
        return new Subscription(() => _listeners.Remove(listener));
    }

    /// <summary>
    ///     Updates the current value and fires all registered <c>OnChange</c> listeners.
    /// </summary>
    public void RaiseChange(T newValue)
    {
        CurrentValue = newValue;

        foreach (var listener in _listeners)
        {
            listener(newValue, null);
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose()
        {
            unsubscribe();
        }
    }
}
