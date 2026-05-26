using System;
using Microsoft.Extensions.Options;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IOptionsMonitor{TOptions}" /> that returns a fixed value
///     and ignores change registrations.
/// </summary>
/// <typeparam name="T">
///     The options type.
/// </typeparam>
public sealed class StubOptionsMonitor<T> : IOptionsMonitor<T>
{
    /// <inheritdoc />
    public T CurrentValue { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="StubOptionsMonitor{T}" /> with the given fixed value.
    /// </summary>
    /// <param name="currentValue">
    ///     The value returned by <see cref="CurrentValue" /> and <see cref="Get" />.
    /// </param>
    public StubOptionsMonitor(T currentValue)
    {
        CurrentValue = currentValue;
    }

    /// <inheritdoc />
    public T Get(string? name)
    {
        return CurrentValue;
    }

    /// <inheritdoc />
    public IDisposable? OnChange(Action<T, string?> listener)
    {
        return null;
    }
}