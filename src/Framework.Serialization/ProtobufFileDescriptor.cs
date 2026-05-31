using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Describes a single <c>.proto</c> file (subset of
///     <c>google.protobuf.FileDescriptorProto</c>). Top-level messages, enums, and services
///     are scoped by the file's package.
/// </summary>
public sealed class ProtobufFileDescriptor
{
    /// <summary>
    ///     Gets the top-level enum types declared in this file.
    /// </summary>
    public required IReadOnlyList<ProtobufEnumDescriptor> Enums { get; init; }

    /// <summary>
    ///     Gets the top-level message types declared in this file.
    /// </summary>
    public required IReadOnlyList<ProtobufMessageDescriptor> Messages { get; init; }

    /// <summary>
    ///     Gets the file name as recorded in the descriptor (typically the <c>.proto</c>
    ///     file path used at compile time).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the package name (may be empty when the file declares no package).
    /// </summary>
    public required string Package { get; init; }

    /// <summary>
    ///     Gets the services declared in this file.
    /// </summary>
    public required IReadOnlyList<ProtobufServiceDescriptor> Services { get; init; }
}
