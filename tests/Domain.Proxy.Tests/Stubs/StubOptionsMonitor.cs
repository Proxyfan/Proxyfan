using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IOptionsMonitor{TOptions}" /> that supports
///     programmatically triggering change notifications for testing hot-reload scenarios.
/// </summary>
/// <typeparam name="T">
///     The options type.
/// </typeparam>
public sealed class StubOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly List<ChangeListener> _listeners;

    /// <inheritdoc />
    public T CurrentValue { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="StubOptionsMonitor{T}" /> with the given initial value.
    /// </summary>
    /// <param name="initialValue">
    ///     The value returned by <see cref="CurrentValue" /> and <see cref="Get" /> on construction.
    /// </param>
    public StubOptionsMonitor(T initialValue)
    {
        _listeners = new List<ChangeListener>();
        CurrentValue = initialValue;
    }

    /// <inheritdoc />
    public T Get(string? name)
    {
        return CurrentValue;
    }

    /// <inheritdoc />
    public IDisposable? OnChange(Action<T, string?> listener)
    {
        var entry = new ChangeListener(listener);
        _listeners.Add(entry);
        return new Subscription(() => _listeners.Remove(entry));
    }

    /// <summary>
    ///     Updates <see cref="CurrentValue" /> and fires all registered <c>OnChange</c> listeners.
    /// </summary>
    /// <param name="newValue">
    ///     The new options value to apply and broadcast.
    /// </param>
    public void RaiseChange(T newValue)
    {
        CurrentValue = newValue;

        foreach (var entry in _listeners)
        {
            entry.Invoke(newValue);
        }
    }

    private delegate void UnsubscribeDelegate();

    private sealed class ChangeListener
    {
        private readonly Action<T, string?> _listener;

        public ChangeListener(Action<T, string?> listener)
        {
            _listener = listener;
        }

        public void Invoke(T value)
        {
            _listener(value, null);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly UnsubscribeDelegate _unsubscribe;
        private bool _isDisposed;

        public Subscription(UnsubscribeDelegate unsubscribe)
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
}