using Microsoft.Extensions.Options;
using Proxyfan.Domain.Throttling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ThrottleApplier" />.
/// </summary>
public sealed class ThrottleApplierTests
{
    /// <summary>
    ///     Verifies that a null monitor completes immediately without throwing.
    /// </summary>
    [Test]
    public async Task ApplyLatencyAsync_NullMonitor_CompletesImmediately()
    {
        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyLatencyAsync(null, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that a profile with zero latency completes immediately.
    /// </summary>
    [Test]
    public async Task ApplyLatencyAsync_ZeroLatency_CompletesImmediately()
    {
        var profile = new ThrottleProfile("test", new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        });
        var monitor = new StubOptionsMonitor<ThrottleProfile>(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyLatencyAsync(monitor, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that a non-zero latency profile introduces at least the configured delay.
    /// </summary>
    [Test]
    public async Task ApplyLatencyAsync_PositiveLatency_DelaysAtLeastConfiguredDuration()
    {
        var profile = new ThrottleProfile("test", new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.FromMilliseconds(50),
            PacketLossProbability = 0,
        });
        var monitor = new StubOptionsMonitor<ThrottleProfile>(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyLatencyAsync(monitor, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsGreaterThanOrEqualTo(40);
    }

    private sealed class StubOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public StubOptionsMonitor(T value)
        {
            _value = value;
        }

        public T CurrentValue => _value;

        public T Get(string? name)
        {
            return _value;
        }

        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return null;
        }
    }
}
