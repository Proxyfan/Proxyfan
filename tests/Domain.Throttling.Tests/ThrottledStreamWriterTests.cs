using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Throttling.Tests;

/// <summary>
///     Tests for <see cref="ThrottledStreamWriter" />.
/// </summary>
public sealed class ThrottledStreamWriterTests
{
    /// <summary>
    ///     Verifies that a write smaller than capacity completes in a single chunk and writes
    ///     all bytes to the destination.
    /// </summary>
    [Test]
    public async Task WriteAsync_BufferSmallerThanCapacity_WritesAllBytes()
    {
        var bucket = new TokenBucket(1024, 1024 * 1024, TimeProvider.System);
        var writer = new ThrottledStreamWriter(bucket, TimeProvider.System);
        var destination = new MemoryStream();
        var payload = BuildPayload(128);

        await writer.WriteAsync(destination, payload, CancellationToken.None);

        await Assert.That(destination.ToArray().Length).IsEqualTo(payload.Length);
        for (var index = 0; index < payload.Length; index++)
        {
            await Assert.That(destination.ToArray()[index]).IsEqualTo(payload[index]);
        }
    }

    /// <summary>
    ///     Verifies that a write larger than capacity is split into multiple chunks while
    ///     preserving payload contents.
    /// </summary>
    [Test]
    public async Task WriteAsync_BufferLargerThanCapacity_PreservesPayload()
    {
        var bucket = new TokenBucket(64, 1024 * 1024, TimeProvider.System);
        var writer = new ThrottledStreamWriter(bucket, TimeProvider.System, TimeSpan.FromMilliseconds(1));
        var destination = new MemoryStream();
        var payload = BuildPayload(256);

        await writer.WriteAsync(destination, payload, CancellationToken.None);

        var written = destination.ToArray();
        await Assert.That(written.Length).IsEqualTo(payload.Length);
        for (var index = 0; index < payload.Length; index++)
        {
            await Assert.That(written[index]).IsEqualTo(payload[index]);
        }
    }

    /// <summary>
    ///     Verifies that a negative retry delay throws ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public async Task Constructor_WithNegativeRetryDelay_Throws()
    {
        var bucket = new TokenBucket(64, 64, TimeProvider.System);

        await Assert.That(() => new ThrottledStreamWriter(bucket, TimeProvider.System, TimeSpan.FromMilliseconds(-1)))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that a zero retry delay throws ArgumentOutOfRangeException so the
    ///     throttled write loop cannot busy-spin when the bucket is exhausted.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroRetryDelay_Throws()
    {
        var bucket = new TokenBucket(64, 64, TimeProvider.System);

        await Assert.That(() => new ThrottledStreamWriter(bucket, TimeProvider.System, TimeSpan.Zero))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that the default constructor uses a 5ms retry delay and writes the buffer.
    /// </summary>
    [Test]
    public async Task Constructor_WithoutRetryDelay_WritesSuccessfully()
    {
        var bucket = new TokenBucket(1024, 1024 * 1024, TimeProvider.System);
        var writer = new ThrottledStreamWriter(bucket, TimeProvider.System);
        var destination = new MemoryStream();
        var payload = BuildPayload(16);

        await writer.WriteAsync(destination, payload, CancellationToken.None);

        await Assert.That(destination.ToArray().Length).IsEqualTo(payload.Length);
    }

    /// <summary>
    ///     Verifies that a zero-length write performs no work and writes nothing.
    /// </summary>
    [Test]
    public async Task WriteAsync_EmptyBuffer_WritesNothing()
    {
        var bucket = new TokenBucket(64, 64, TimeProvider.System);
        var writer = new ThrottledStreamWriter(bucket, TimeProvider.System);
        var destination = new MemoryStream();

        await writer.WriteAsync(destination, ReadOnlyMemory<byte>.Empty, CancellationToken.None);

        await Assert.That(destination.Length).IsEqualTo(0L);
    }

    private static byte[] BuildPayload(int length)
    {
        var payload = new byte[length];
        for (var index = 0; index < length; index++)
        {
            payload[index] = (byte)(index & 0xFF);
        }

        return payload;
    }
}
