using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Describes a protobuf service (subset of <c>google.protobuf.ServiceDescriptorProto</c>).
/// </summary>
public sealed class ProtobufServiceDescriptor
{
    /// <summary>
    ///     Gets the fully qualified name (e.g. <c>".foo.Greeter"</c>).
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    ///     Gets the methods declared on this service.
    /// </summary>
    public required IReadOnlyList<ProtobufMethodDescriptor> Methods { get; init; }

    /// <summary>
    ///     Gets the service's local name (e.g. <c>"Greeter"</c>).
    /// </summary>
    public required string Name { get; init; }
}
