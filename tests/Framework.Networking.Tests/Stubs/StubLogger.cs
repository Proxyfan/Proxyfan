using System;
using Microsoft.Extensions.Logging;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>A no-op stub implementation of <see cref="ILogger{TCategoryName}" /> for testing.</summary>
/// <typeparam name="T">The category type.</typeparam>
internal sealed class StubLogger<T> : ILogger<T>
{
    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return new NoopScope();
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    private sealed class NoopScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
