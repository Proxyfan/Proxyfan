using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Throttling;

/// <summary>
///     Performs throttled writes by consuming tokens from a <see cref="TokenBucket" /> before
///     each chunk is written. When the bucket is exhausted, the writer awaits a short delay
///     and retries until all bytes have been written.
/// </summary>
public sealed class ThrottledStreamWriter
{
    private readonly TokenBucket _bucket;
    private readonly TimeSpan _retryDelay;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new <see cref="ThrottledStreamWriter" /> with a 5 ms retry delay.
    /// </summary>
    /// <param name="bucket">The token bucket used to limit throughput.</param>
    /// <param name="timeProvider">The time provider used for delays.</param>
    public ThrottledStreamWriter(TokenBucket bucket, TimeProvider timeProvider)
        : this(bucket, timeProvider, TimeSpan.FromMilliseconds(5))
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="ThrottledStreamWriter" /> with the supplied retry delay.
    /// </summary>
    /// <param name="bucket">The token bucket used to limit throughput.</param>
    /// <param name="timeProvider">The time provider used for delays.</param>
    /// <param name="retryDelay">The delay between bucket-empty retries.</param>
    public ThrottledStreamWriter(TokenBucket bucket, TimeProvider timeProvider, TimeSpan retryDelay)
    {
        if (retryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay), retryDelay, "Retry delay must be non-negative.");
        }

        _bucket = bucket;
        _timeProvider = timeProvider;
        _retryDelay = retryDelay;
    }

    /// <summary>
    ///     Writes the supplied buffer to the destination stream while consuming tokens from the
    ///     underlying bucket. The buffer is written in chunks no larger than the bucket capacity.
    /// </summary>
    /// <param name="destination">The destination stream.</param>
    /// <param name="buffer">The buffer to write.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the buffer has been fully written.</returns>
    public async Task WriteAsync(Stream destination, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        var offset = 0;

        while (offset < buffer.Length)
        {
            var remaining = buffer.Length - offset;
            var chunkSize = (int)Math.Min(remaining, _bucket.Capacity);

            while (!_bucket.CanConsume(chunkSize))
            {
                await Task.Delay(_retryDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }

            await destination.WriteAsync(buffer.Slice(offset, chunkSize), cancellationToken).ConfigureAwait(false);
            offset += chunkSize;
        }
    }
}
