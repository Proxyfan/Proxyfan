namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Protobuf wire types as defined by https://protobuf.dev/programming-guides/encoding/.
/// </summary>
public enum ProtobufWireType
{
    /// <summary>
    ///     VARINT (0): int32, int64, uint32, uint64, sint32, sint64, bool, enum.
    /// </summary>
    Varint = 0,

    /// <summary>
    ///     I64 (1): fixed64, sfixed64, double.
    /// </summary>
    I64 = 1,

    /// <summary>
    ///     LEN (2): string, bytes, embedded messages, packed repeated fields.
    /// </summary>
    LengthDelimited = 2,

    /// <summary>
    ///     SGROUP (3): start group (deprecated).
    /// </summary>
    StartGroup = 3,

    /// <summary>
    ///     EGROUP (4): end group (deprecated).
    /// </summary>
    EndGroup = 4,

    /// <summary>
    ///     I32 (5): fixed32, sfixed32, float.
    /// </summary>
    I32 = 5,
}
