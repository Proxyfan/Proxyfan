using Microsoft.Extensions.Logging;
using System;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A no-op <see cref="ILoggerFactory" /> that returns <see cref="StubLogger{T}" /> instances.
///     Used by tests that need to construct services taking an <see cref="ILoggerFactory" />.
/// </summary>
public sealed class StubLoggerFactory : ILoggerFactory
{
    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
        _ = provider;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        _ = categoryName;
        return new TypedStubLogger();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private sealed class TypedStubLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            _ = state;
            return new NoopScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            _ = logLevel;
            return false;
        }

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
}
