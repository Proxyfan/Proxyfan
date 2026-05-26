using System;
using System.Threading;

namespace Proxyfan.Domain.Throttling;

/// <summary>
///     A thread-safe token-bucket bandwidth limiter. Each token represents one byte. Tokens
///     accumulate at a configurable rate and are consumed by calls to <see cref="CanConsume" />.
///     The bucket is capped at <see cref="Capacity" /> tokens to bound burstiness.
/// </summary>
public sealed class TokenBucket
{
    private readonly Lock _syncRoot;
    private readonly TimeProvider _timeProvider;
    private long _availableTokens;
    private long _lastRefillTimestamp;

    /// <summary>
    ///     Gets the maximum number of tokens the bucket can hold.
    /// </summary>
    public long Capacity { get; }

    /// <summary>
    ///     Gets the configured token refill rate in tokens per second.
    /// </summary>
    public long RefillRatePerSecond { get; }

    /// <summary>
    ///     Initializes a new <see cref="TokenBucket" /> at full capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of tokens the bucket can hold.</param>
    /// <param name="refillRatePerSecond">The token refill rate per second.</param>
    /// <param name="timeProvider">The time provider used for refill calculations.</param>
    public TokenBucket(long capacity, long refillRatePerSecond, TimeProvider timeProvider)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        if (refillRatePerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(refillRatePerSecond), refillRatePerSecond, "Refill rate must be positive.");
        }

        Capacity = capacity;
        RefillRatePerSecond = refillRatePerSecond;
        _timeProvider = timeProvider;
        _availableTokens = capacity;
        var syncRoot = new Lock();
        _syncRoot = syncRoot;
        _lastRefillTimestamp = timeProvider.GetTimestamp();
    }

    /// <summary>
    ///     Attempts to consume the requested number of tokens. Returns true on success and false
    ///     when insufficient tokens are available (no partial consumption).
    /// </summary>
    /// <param name="requestedTokens">The number of tokens to consume.</param>
    /// <returns><see langword="true" /> when the consumption succeeded.</returns>
    public bool CanConsume(long requestedTokens)
    {
        if (requestedTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedTokens), requestedTokens, "Requested tokens must be positive.");
        }

        lock (_syncRoot)
        {
            Refill();

            if (_availableTokens < requestedTokens)
            {
                return false;
            }

            _availableTokens -= requestedTokens;
            return true;
        }
    }

    /// <summary>
    ///     Returns the number of tokens currently available without consuming any.
    /// </summary>
    /// <returns>The current token count after any pending refill.</returns>
    public long GetAvailableTokens()
    {
        lock (_syncRoot)
        {
            Refill();
            return _availableTokens;
        }
    }

    private void Refill()
    {
        var now = _timeProvider.GetTimestamp();
        var elapsed = _timeProvider.GetElapsedTime(_lastRefillTimestamp, now);
        var tokensToAdd = (long)(elapsed.TotalSeconds * RefillRatePerSecond);

        if (tokensToAdd <= 0)
        {
            return;
        }

        _availableTokens = Math.Min(Capacity, _availableTokens + tokensToAdd);
        _lastRefillTimestamp = now;
    }
}
