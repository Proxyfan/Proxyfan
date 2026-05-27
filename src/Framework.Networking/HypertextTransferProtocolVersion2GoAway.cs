using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Result of parsing the payload of an HTTP/2 GOAWAY frame (RFC 7540 § 6.8).
/// </summary>
public readonly record struct HypertextTransferProtocolVersion2GoAway
{
    /// <summary>
    ///     Gets the additional debug data (zero-length when absent).
    /// </summary>
    public ReadOnlyMemory<byte> AdditionalDebugData { get; }

    /// <summary>
    ///     Gets the 32-bit error code describing the reason for connection termination.
    /// </summary>
    public uint ErrorCode { get; }

    /// <summary>
    ///     Gets the highest stream identifier that the sender of the GOAWAY frame might have
    ///     acted upon. Streams with greater identifiers should be considered unprocessed.
    /// </summary>
    public uint LastStreamIdentifier { get; }

    /// <summary>
    ///     Initializes a new GOAWAY parse result.
    /// </summary>
    /// <param name="lastStreamIdentifier">The last stream identifier processed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="additionalDebugData">Optional debug data (empty when absent).</param>
    public HypertextTransferProtocolVersion2GoAway(uint lastStreamIdentifier, uint errorCode, ReadOnlyMemory<byte> additionalDebugData)
    {
        LastStreamIdentifier = lastStreamIdentifier;
        ErrorCode = errorCode;
        AdditionalDebugData = additionalDebugData;
    }
}
