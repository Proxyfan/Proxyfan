using System;
using Microsoft.Extensions.Options;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IOptionsMonitor{TOptions}" /> that returns a fixed value.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
/// <param name="currentValue">The value returned by <see cref="IOptionsMonitor{TOptions}.CurrentValue" /> and <see cref="Get" />.</param>
internal sealed class StubOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
{
    /// <inheritdoc />
    public T CurrentValue { get; } = currentValue;

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
