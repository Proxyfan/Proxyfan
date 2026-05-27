namespace Proxyfan.Framework.Serialization;

/// <summary>
///     A single decoded protobuf field. The interpretation of <see cref="Value" /> depends
///     on <see cref="WireType" />:
///     <list type="bullet">
///       <item><see cref="ProtobufWireType.Varint" />: <see cref="ulong" />.</item>
///       <item><see cref="ProtobufWireType.I64" />: <see cref="ulong" /> (raw little-endian bytes).</item>
///       <item><see cref="ProtobufWireType.I32" />: <see cref="uint" /> (raw little-endian bytes).</item>
///       <item><see cref="ProtobufWireType.LengthDelimited" />: <c>byte[]</c>.</item>
///     </list>
/// </summary>
public sealed class ProtobufField
{
    /// <summary>
    ///     Gets the field number (the integer tag in the .proto definition).
    /// </summary>
    public int FieldNumber { get; }

    /// <summary>
    ///     Gets the raw decoded value (boxed primitive or byte array; see class docs).
    /// </summary>
    public object Value { get; }

    /// <summary>
    ///     Gets the wire type that determined the value's storage shape.
    /// </summary>
    public ProtobufWireType WireType { get; }

    /// <summary>
    ///     Initializes a new <see cref="ProtobufField" />.
    /// </summary>
    /// <param name="fieldNumber">The field number.</param>
    /// <param name="wireType">The wire type.</param>
    /// <param name="value">The decoded value.</param>
    public ProtobufField(int fieldNumber, ProtobufWireType wireType, object value)
    {
        FieldNumber = fieldNumber;
        WireType = wireType;
        Value = value;
    }
}
