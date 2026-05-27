namespace Proxyfan.Framework.Networking;

/// <summary>
///     HTTP/2 SETTINGS parameter identifiers per RFC 7540 § 6.5.2. Unknown identifiers must
///     be ignored per the specification; new values may be added in the future via the
///     HTTP/2 IANA registry.
/// </summary>
public enum HypertextTransferProtocolVersion2SettingIdentifier
{
    /// <summary>
    ///     SETTINGS_HEADER_TABLE_SIZE (0x1): the maximum size of the HPACK header
    ///     compression table used to decode header blocks.
    /// </summary>
    HeaderTableSize = 0x1,

    /// <summary>
    ///     SETTINGS_ENABLE_PUSH (0x2): a flag that disables server push when zero.
    /// </summary>
    EnablePush = 0x2,

    /// <summary>
    ///     SETTINGS_MAX_CONCURRENT_STREAMS (0x3): the maximum number of concurrent streams
    ///     the sender will allow.
    /// </summary>
    MaximumConcurrentStreams = 0x3,

    /// <summary>
    ///     SETTINGS_INITIAL_WINDOW_SIZE (0x4): the sender's initial window size for stream-level
    ///     flow control.
    /// </summary>
    InitialWindowSize = 0x4,

    /// <summary>
    ///     SETTINGS_MAX_FRAME_SIZE (0x5): the maximum frame payload size the sender is willing
    ///     to receive.
    /// </summary>
    MaximumFrameSize = 0x5,

    /// <summary>
    ///     SETTINGS_MAX_HEADER_LIST_SIZE (0x6): an advisory maximum on the size of header lists
    ///     the sender is prepared to accept.
    /// </summary>
    MaximumHeaderListSize = 0x6,
}
