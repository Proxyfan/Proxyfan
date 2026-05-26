using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Throttling.Tests;

/// <summary>
///     Tests for <see cref="TokenBucket" />.
/// </summary>
public sealed class TokenBucketTests
{
    /// <summary>
    ///     Verifies that the constructor stores capacity and refill rate.
    /// </summary>
    [Test]
    public async Task Constructor_WithValidArguments_StoresCapacityAndRefillRate()
    {
        var bucket = new TokenBucket(1000, 500, TimeProvider.System);

        await Assert.That(bucket.Capacity).IsEqualTo(1000L);
        await Assert.That(bucket.RefillRatePerSecond).IsEqualTo(500L);
    }

    /// <summary>
    ///     Verifies that <see cref="TokenBucket.GetAvailableTokens" /> starts at full capacity.
    /// </summary>
    [Test]
    public async Task GetAvailableTokens_AfterConstruction_StartsFullCapacity()
    {
        var bucket = new TokenBucket(1000, 500, TimeProvider.System);

        var available = bucket.GetAvailableTokens();

        await Assert.That(available).IsEqualTo(1000L);
    }

    /// <summary>
    ///     Verifies that consuming fewer tokens than available succeeds and reduces the count.
    /// </summary>
    [Test]
    public async Task CanConsume_WithSufficientTokens_SucceedsAndReducesCount()
    {
        var bucket = new TokenBucket(1000, 500, TimeProvider.System);

        var consumed = bucket.CanConsume(400);

        await Assert.That(consumed).IsTrue();
        await Assert.That(bucket.GetAvailableTokens()).IsGreaterThanOrEqualTo(600L);
    }

    /// <summary>
    ///     Verifies that requesting more tokens than available fails without partial consumption.
    /// </summary>
    [Test]
    public async Task CanConsume_WithInsufficientTokens_ReturnsFalse()
    {
        var bucket = new TokenBucket(100, 50, TimeProvider.System);

        var consumed = bucket.CanConsume(200);

        await Assert.That(consumed).IsFalse();
        await Assert.That(bucket.GetAvailableTokens()).IsEqualTo(100L);
    }

    /// <summary>
    ///     Verifies that the constructor rejects a non-positive capacity.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroCapacity_Throws()
    {
        await Assert.That(() => _ = new TokenBucket(0, 500, TimeProvider.System)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that the constructor rejects a non-positive refill rate.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroRefillRate_Throws()
    {
        await Assert.That(() => _ = new TokenBucket(100, 0, TimeProvider.System)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that <see cref="Tokenbucket.CanConsume" /> rejects non-positive requests.
    /// </summary>
    [Test]
    public async Task CanConsume_WithZeroRequest_Throws()
    {
        var bucket = new TokenBucket(100, 50, TimeProvider.System);

        await Assert.That(() => bucket.CanConsume(0)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that tokens refill over time. Uses a fake <see cref="TimeProvider" /> for determinism.
    /// </summary>
    [Test]
    public async Task GetAvailableTokens_AfterRefillElapsed_AddsTokens()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        var bucket = new TokenBucket(1000, 100, fakeTimeProvider);
        bucket.CanConsume(800);
        // After consume: ~200 tokens available

        fakeTimeProvider.AdvanceBy(TimeSpan.FromSeconds(5));
        var available = bucket.GetAvailableTokens();

        // After 5 seconds at 100 tokens/sec: 500 more tokens, but capped at capacity 1000.
        // So we expect at least 200 + (5*100) = 700 available (could be capped at 1000).
        await Assert.That(available).IsGreaterThanOrEqualTo(700L);
    }

    /// <summary>
    ///     Verifies that the bucket caps refill at capacity (no overflow above the bucket size).
    /// </summary>
    [Test]
    public async Task GetAvailableTokens_AfterLongRefill_CapsAtCapacity()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        var bucket = new TokenBucket(100, 50, fakeTimeProvider);
        bucket.CanConsume(100);
        // Now 0 tokens.

        fakeTimeProvider.AdvanceBy(TimeSpan.FromSeconds(10));
        var available = bucket.GetAvailableTokens();

        // At 50/sec for 10s, refill would be 500 but capacity is 100.
        await Assert.That(available).IsEqualTo(100L);
    }

    /// <summary>
    ///     A fake <see cref="TimeProvider" /> that returns deterministic timestamps under test control.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private long _currentTimestamp;

        public override long TimestampFrequency => 1_000_000_000;

        public override long GetTimestamp()
        {
            return _currentTimestamp;
        }

        public void AdvanceBy(TimeSpan elapsed)
        {
            _currentTimestamp += (long)(elapsed.TotalSeconds * TimestampFrequency);
        }
    }
}
