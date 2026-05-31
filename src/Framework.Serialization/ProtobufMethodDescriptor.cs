namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Describes a single RPC method on a protobuf service (subset of
///     <c>google.protobuf.MethodDescriptorProto</c>).
/// </summary>
public sealed class ProtobufMethodDescriptor
{
    /// <summary>
    ///     Gets the gRPC path used on the wire (<c>/package.Service/Method</c>).
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    ///     Gets the fully qualified name of the request message type
    ///     (e.g. <c>".foo.HelloRequest"</c>).
    /// </summary>
    public required string InputType { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the client streams request messages.
    /// </summary>
    public required bool IsClientStreaming { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the server streams response messages.
    /// </summary>
    public required bool IsServerStreaming { get; init; }

    /// <summary>
    ///     Gets the method's local name (e.g. <c>"SayHello"</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the fully qualified name of the response message type
    ///     (e.g. <c>".foo.HelloReply"</c>).
    /// </summary>
    public required string OutputType { get; init; }
}
