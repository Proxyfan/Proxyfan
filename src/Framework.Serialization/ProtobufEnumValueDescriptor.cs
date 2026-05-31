namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Describes a single value of a protobuf enum (subset of
///     <c>google.protobuf.EnumValueDescriptorProto</c>).
/// </summary>
public sealed class ProtobufEnumValueDescriptor
{
    /// <summary>
    ///     Gets the enum value's symbolic name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the enum value's numeric identifier.
    /// </summary>
    public required int Number { get; init; }
}
