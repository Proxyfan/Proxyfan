using System;
using System.Threading;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Tracks the shared byte budget used by streaming protocol captures.
/// </summary>
public sealed class StreamingCaptureBudget
{
    private readonly Lock _syncRoot;
    private long _reservedBytes;

    /// <summary>
    ///     Gets the maximum number of bytes that may be reserved.
    /// </summary>
    public long CapacityBytes { get; }

    /// <summary>
    ///     Gets the currently reserved byte count.
    /// </summary>
    public long ReservedBytes
    {
        get
        {
            lock (_syncRoot)
            {
                return _reservedBytes;
            }
        }
    }

    /// <summary>
    ///     Initializes a new <see cref="StreamingCaptureBudget" />.
    /// </summary>
    /// <param name="capacityBytes">The maximum number of bytes that can be reserved.</param>
    public StreamingCaptureBudget(long capacityBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacityBytes, 0L);

        CapacityBytes = capacityBytes;
        _reservedBytes = 0L;
        var syncRoot = new Lock();
        _syncRoot = syncRoot;
    }

    /// <summary>
    ///     Attempts an atomic replace operation that releases a previous allocation and reserves
    ///     a new allocation in one step.
    /// </summary>
    /// <param name="releasedBytes">The old allocation to release.</param>
    /// <param name="reservedBytes">The new allocation to reserve.</param>
    /// <returns><see langword="true" /> when the replace succeeded.</returns>
    public bool CanReplaceReservation(int releasedBytes, int reservedBytes)
    {
        if (reservedBytes <= 0)
        {
            Release(releasedBytes);
            return true;
        }

        if (releasedBytes < 0)
        {
            releasedBytes = 0;
        }

        lock (_syncRoot)
        {
            var adjustedReservedBytes = _reservedBytes - releasedBytes;
            if (adjustedReservedBytes < 0L)
            {
                adjustedReservedBytes = 0L;
            }

            if (adjustedReservedBytes > CapacityBytes - reservedBytes)
            {
                return false;
            }

            _reservedBytes = adjustedReservedBytes + reservedBytes;
            return true;
        }
    }

    /// <summary>
    ///     Attempts to reserve the supplied number of bytes.
    /// </summary>
    /// <param name="bytes">The number of bytes to reserve.</param>
    /// <returns><see langword="true" /> when the reservation succeeded.</returns>
    public bool CanReserve(int bytes)
    {
        if (bytes <= 0)
        {
            return true;
        }

        lock (_syncRoot)
        {
            if (_reservedBytes > CapacityBytes - bytes)
            {
                return false;
            }

            _reservedBytes += bytes;
            return true;
        }
    }

    /// <summary>
    ///     Releases the supplied number of bytes.
    /// </summary>
    /// <param name="bytes">The number of bytes to release.</param>
    public void Release(int bytes)
    {
        if (bytes <= 0)
        {
            return;
        }

        lock (_syncRoot)
        {
            _reservedBytes -= bytes;
            if (_reservedBytes < 0L)
            {
                _reservedBytes = 0L;
            }
        }
    }
}
