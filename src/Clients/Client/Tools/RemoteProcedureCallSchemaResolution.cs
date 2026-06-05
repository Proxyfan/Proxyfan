namespace Proxyfan.Client.Tools;

/// <summary>
///     Optional schema metadata used for schema-aware gRPC payload rendering.
/// </summary>
public sealed class RemoteProcedureCallSchemaResolution
{
    /// <summary>
    ///     Gets an opaque descriptor index token used by serializer adapters.
    /// </summary>
    public object? IndexToken { get; init; }

    /// <summary>
    ///     Gets the schema display name.
    /// </summary>
    public string? SchemaFullName { get; init; }

    /// <summary>
    ///     Gets an opaque schema token used by serializer adapters.
    /// </summary>
    public object? SchemaToken { get; init; }
}
