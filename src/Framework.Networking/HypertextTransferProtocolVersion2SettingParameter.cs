namespace Proxyfan.Framework.Networking;

/// <summary>
///     A single HTTP/2 SETTINGS parameter — a 16-bit identifier plus its 32-bit value.
/// </summary>
public sealed class HypertextTransferProtocolVersion2SettingParameter
{
    /// <summary>
    ///     Gets the parameter identifier (some identifiers may not match a known enum value;
    ///     unknown identifiers must still be skipped over per RFC 7540 § 6.5.2).
    /// </summary>
    public ushort Identifier { get; }

    /// <summary>
    ///     Gets a value indicating whether <see cref="Identifier" /> is a known parameter
    ///     defined by RFC 7540.
    /// </summary>
    public bool IsKnownIdentifier => Identifier is >= 0x1 and <= 0x6;

    /// <summary>
    ///     Gets the strongly-typed identifier; the value is cast even when
    ///     <see cref="IsKnownIdentifier" /> is false to allow simple equality checks.
    /// </summary>
    public HypertextTransferProtocolVersion2SettingIdentifier KnownIdentifier =>
        (HypertextTransferProtocolVersion2SettingIdentifier)Identifier;

    /// <summary>
    ///     Gets the 32-bit parameter value.
    /// </summary>
    public uint Value { get; }

    /// <summary>
    ///     Initializes a new SETTINGS parameter.
    /// </summary>
    /// <param name="identifier">The 16-bit identifier.</param>
    /// <param name="value">The 32-bit value.</param>
    public HypertextTransferProtocolVersion2SettingParameter(ushort identifier, uint value)
    {
        Identifier = identifier;
        Value = value;
    }
}
