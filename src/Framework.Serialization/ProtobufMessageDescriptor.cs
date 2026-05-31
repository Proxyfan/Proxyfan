using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Describes a protobuf message type (subset of <c>google.protobuf.DescriptorProto</c>).
///     Messages may declare nested message and enum types, both of which are included as
///     descendants of this descriptor.
/// </summary>
public sealed class ProtobufMessageDescriptor
{
    /// <summary>
    ///     Gets the message's fields in declaration order.
    /// </summary>
    public required IReadOnlyList<ProtobufFieldDescriptor> Fields { get; init; }

    /// <summary>
    ///     Gets the fully qualified name (e.g. <c>".foo.HelloRequest"</c>).
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    ///     Gets the message's local name as declared in the <c>.proto</c> file.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the nested enum types declared inside this message.
    /// </summary>
    public required IReadOnlyList<ProtobufEnumDescriptor> NestedEnums { get; init; }

    /// <summary>
    ///     Gets the nested message types declared inside this message.
    /// </summary>
    public required IReadOnlyList<ProtobufMessageDescriptor> NestedMessages { get; init; }
}
