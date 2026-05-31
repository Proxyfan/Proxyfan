namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Mirrors <c>google.protobuf.FieldDescriptorProto.Type</c> from <c>descriptor.proto</c>.
///     Identifies the scalar or compound type of a protobuf field declared in a
///     <c>.proto</c> file. The <c>Type</c> prefix on each value matches the
///     <c>TYPE_*</c> naming used in the protobuf wire spec and avoids collisions with
///     .NET BCL type names.
/// </summary>
public enum ProtobufFieldKind
{
    /// <summary>
    ///     <c>TYPE_DOUBLE</c> (1): 64-bit IEEE 754 floating point.
    /// </summary>
    TypeDouble = 1,

    /// <summary>
    ///     <c>TYPE_FLOAT</c> (2): 32-bit IEEE 754 floating point.
    /// </summary>
    TypeFloat = 2,

    /// <summary>
    ///     <c>TYPE_INT64</c> (3): variable-length signed 64-bit integer.
    /// </summary>
    TypeInt64 = 3,

    /// <summary>
    ///     <c>TYPE_UINT64</c> (4): variable-length unsigned 64-bit integer.
    /// </summary>
    TypeUInt64 = 4,

    /// <summary>
    ///     <c>TYPE_INT32</c> (5): variable-length signed 32-bit integer.
    /// </summary>
    TypeInt32 = 5,

    /// <summary>
    ///     <c>TYPE_FIXED64</c> (6): fixed-length 64-bit unsigned integer.
    /// </summary>
    TypeFixed64 = 6,

    /// <summary>
    ///     <c>TYPE_FIXED32</c> (7): fixed-length 32-bit unsigned integer.
    /// </summary>
    TypeFixed32 = 7,

    /// <summary>
    ///     <c>TYPE_BOOL</c> (8): boolean.
    /// </summary>
    TypeBool = 8,

    /// <summary>
    ///     <c>TYPE_STRING</c> (9): UTF-8 string.
    /// </summary>
    TypeString = 9,

    /// <summary>
    ///     <c>TYPE_GROUP</c> (10): deprecated group encoding (Proxyfan does not decode this).
    /// </summary>
    TypeGroup = 10,

    /// <summary>
    ///     <c>TYPE_MESSAGE</c> (11): embedded message (see <c>TypeName</c>).
    /// </summary>
    TypeMessage = 11,

    /// <summary>
    ///     <c>TYPE_BYTES</c> (12): arbitrary byte string.
    /// </summary>
    TypeBytes = 12,

    /// <summary>
    ///     <c>TYPE_UINT32</c> (13): variable-length unsigned 32-bit integer.
    /// </summary>
    TypeUInt32 = 13,

    /// <summary>
    ///     <c>TYPE_ENUM</c> (14): enum value (see <c>TypeName</c>).
    /// </summary>
    TypeEnum = 14,

    /// <summary>
    ///     <c>TYPE_SFIXED32</c> (15): fixed-length signed 32-bit integer.
    /// </summary>
    TypeSignedFixed32 = 15,

    /// <summary>
    ///     <c>TYPE_SFIXED64</c> (16): fixed-length signed 64-bit integer.
    /// </summary>
    TypeSignedFixed64 = 16,

    /// <summary>
    ///     <c>TYPE_SINT32</c> (17): zig-zag-encoded signed 32-bit integer.
    /// </summary>
    TypeSignedInt32 = 17,

    /// <summary>
    ///     <c>TYPE_SINT64</c> (18): zig-zag-encoded signed 64-bit integer.
    /// </summary>
    TypeSignedInt64 = 18,
}