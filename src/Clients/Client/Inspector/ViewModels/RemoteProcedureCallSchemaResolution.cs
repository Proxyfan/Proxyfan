using Proxyfan.Framework.Serialization;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     Result of resolving a captured gRPC message against an
///     <see cref="IRemoteProcedureCallDescriptorLibrary" />. Both properties are
///     <see langword="null" /> when no descriptor library is configured or the gRPC method
///     path does not resolve to a method descriptor.
/// </summary>
public sealed class RemoteProcedureCallSchemaResolution
{
    /// <summary>
    ///     Gets the descriptor index used to resolve nested types when rendering the schema.
    /// </summary>
    public ProtobufDescriptorIndex? Index { get; init; }

    /// <summary>
    ///     Gets the message descriptor for the captured message (input type for outbound,
    ///     output type for inbound).
    /// </summary>
    public ProtobufMessageDescriptor? Schema { get; init; }
}
