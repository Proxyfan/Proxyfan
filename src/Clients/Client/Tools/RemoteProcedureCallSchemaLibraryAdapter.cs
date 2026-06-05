using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Adapts framework descriptor metadata to a client-owned schema library contract.
/// </summary>
public sealed class RemoteProcedureCallSchemaLibraryAdapter : IRemoteProcedureCallSchemaLibrary
{
    private readonly IRemoteProcedureCallDescriptorLibrary _library;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RemoteProcedureCallSchemaLibraryAdapter" /> class.
    /// </summary>
    /// <param name="library">The framework descriptor library.</param>
    public RemoteProcedureCallSchemaLibraryAdapter(IRemoteProcedureCallDescriptorLibrary library)
    {
        _library = library;
    }

    /// <inheritdoc />
    public RemoteProcedureCallSchemaResolution Resolve(string? methodPath, RemoteProcedureCallDirection direction)
    {
        if (methodPath is null)
        {
            return new RemoteProcedureCallSchemaResolution();
        }

        var index = _library.Index;
        var method = index.TryResolveMethod(methodPath);
        if (method is null)
        {
            return new RemoteProcedureCallSchemaResolution();
        }

        var typeName = direction == RemoteProcedureCallDirection.Outbound
            ? method.InputType
            : method.OutputType;
        var schema = index.TryResolveMessage(typeName);
        if (schema is null)
        {
            return new RemoteProcedureCallSchemaResolution();
        }

        var resolution = new RemoteProcedureCallSchemaResolution
        {
            IndexToken = index,
            SchemaFullName = schema.FullName,
            SchemaToken = schema,
        };
        return resolution;
    }
}
