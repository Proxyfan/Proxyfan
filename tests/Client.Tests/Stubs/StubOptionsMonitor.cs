using Microsoft.Extensions.Options;
using System;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     A stub <see cref="IOptionsMonitor{TOptions}" /> that always returns a fixed
///     options value, for use in unit tests.
/// </summary>
/// <typeparam name="TOptions">
///     The type of options returned.
/// </typeparam>
internal sealed class StubOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
{
    private readonly TOptions _value;

    /// <inheritdoc />
    public TOptions CurrentValue => _value;

    /// <summary>
    ///     Initializes a new instance of <see cref="StubOptionsMonitor{TOptions}" />.
    /// </summary>
    /// <param name="value">The fixed options value to return.</param>
    public StubOptionsMonitor(TOptions value)
    {
        _value = value;
    }

    /// <inheritdoc />
    public TOptions Get(string? name)
    {
        return _value;
    }

    /// <inheritdoc />
    public IDisposable? OnChange(Action<TOptions, string?> listener)
    {
        return null;
    }
}