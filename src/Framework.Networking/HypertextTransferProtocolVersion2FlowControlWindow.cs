using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     HTTP/2 flow-control window per RFC 7540 § 5.2. A window represents a per-stream or
///     per-connection budget of bytes that may be sent before a WINDOW_UPDATE is received.
///     Windows may legally go negative if the peer reduces SETTINGS_INITIAL_WINDOW_SIZE
///     after frames are in flight; senders must wait until the window is non-negative
///     before transmitting more DATA bytes.
/// </summary>
public sealed class HypertextTransferProtocolVersion2FlowControlWindow
{
    /// <summary>
    ///     The default initial window size per RFC 7540 § 6.9.2 (65 535 octets).
    /// </summary>
    public const int DefaultInitialSize = 65535;

    /// <summary>
    ///     The maximum window size per RFC 7540 § 6.9.1 (2^31 - 1 octets).
    /// </summary>
    public const int MaximumSize = int.MaxValue;

    /// <summary>
    ///     Gets the number of octets the sender is currently permitted to transmit before
    ///     it must wait for a WINDOW_UPDATE.
    /// </summary>
    public int Available { get; private set; }

    /// <summary>
    ///     Initializes the window to <see cref="DefaultInitialSize" /> octets.
    /// </summary>
    public HypertextTransferProtocolVersion2FlowControlWindow()
        : this(DefaultInitialSize)
    {
    }

    /// <summary>
    ///     Initializes the window to an explicit starting size.
    /// </summary>
    /// <param name="initialSize">The starting window size (may be 0; must not exceed <see cref="MaximumSize" />).</param>
    public HypertextTransferProtocolVersion2FlowControlWindow(int initialSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialSize);
        Available = initialSize;
    }

    /// <summary>
    ///     Applies a SETTINGS_INITIAL_WINDOW_SIZE update by adjusting the window by the delta between
    ///     the new and old initial sizes. May result in a negative <see cref="Available" /> value if
    ///     the peer shrinks the window while in-flight DATA has not yet been acknowledged.
    /// </summary>
    /// <param name="delta">The signed difference between the new and old initial window sizes.</param>
    /// <returns>
    ///     <c>true</c> when the delta was applied; <c>false</c> when the adjusted window would
    ///     fall outside the legal range — for a positive overflow this is a FLOW_CONTROL_ERROR
    ///     per RFC 7540 § 6.9.2, and for a negative underflow it represents an unrepresentable
    ///     window that the caller must surface as a connection error. The window is left
    ///     unchanged when <c>false</c> is returned.
    /// </returns>
    public bool HasAppliedInitialSizeDelta(int delta)
    {
        var sum = (long)Available + delta;
        if (sum > MaximumSize)
        {
            return false;
        }
        if (sum < int.MinValue)
        {
            return false;
        }
        Available = (int)sum;
        return true;
    }

    /// <summary>
    ///     Attempts to consume <paramref name="octets" /> from the window. Returns <c>false</c>
    ///     and leaves the window untouched when the window is too small.
    /// </summary>
    /// <param name="octets">The number of octets to consume; must be non-negative.</param>
    /// <returns><c>true</c> when the window had sufficient budget and was decremented; otherwise <c>false</c>.</returns>
    public bool HasConsumed(int octets)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(octets);
        if (octets > Available)
        {
            return false;
        }
        Available -= octets;
        return true;
    }

    /// <summary>
    ///     Increments the window by <paramref name="increment" /> as defined by a WINDOW_UPDATE frame.
    /// </summary>
    /// <param name="increment">A positive WINDOW_UPDATE delta.</param>
    /// <returns>
    ///     <c>true</c> on success; <c>false</c> when the increment would cause the window to exceed
    ///     <see cref="MaximumSize" /> (a FLOW_CONTROL_ERROR per RFC 7540 § 6.9.1).
    /// </returns>
    public bool HasIncremented(int increment)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(increment);
        if (increment == 0)
        {
            return false;
        }
        var sum = (long)Available + increment;
        if (sum > MaximumSize)
        {
            return false;
        }
        Available = (int)sum;
        return true;
    }
}
