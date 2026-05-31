using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Describes a protobuf enum type (subset of <c>google.protobuf.EnumDescriptorProto</c>).
/// </summary>
public sealed class ProtobufEnumDescriptor
{
    /// <summary>
    ///     Gets the fully qualified name (e.g. <c>".foo.Color"</c>).
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    ///     Gets the enum's local name as declared in the <c>.proto</c> file.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the values declared for this enum.
    /// </summary>
    public required IReadOnlyList<ProtobufEnumValueDescriptor> Values { get; init; }
}
