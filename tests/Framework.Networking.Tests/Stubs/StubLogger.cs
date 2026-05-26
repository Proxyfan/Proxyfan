using System;
using Microsoft.Extensions.Logging;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A no-op stub implementation of <see cref="ILogger{TCategoryName}" /> that discards all
///     log output, used in unit tests where logging side-effects are irrelevant.
/// </summary>
/// <typeparam name="T">
///     The category type.
/// </typeparam>
public sealed class StubLogger<T> : ILogger<T>
{
    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        var scope = new NoopScope();
        return scope;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _ = (logLevel, eventId, state, exception, formatter);
    }

    private sealed class NoopScope : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}