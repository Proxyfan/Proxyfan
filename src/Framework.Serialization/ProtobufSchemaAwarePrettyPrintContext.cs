using System.Text;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Bundled context used by <see cref="ProtobufSchemaAwarePrettyPrinter" /> to keep its
///     helper methods under the analyzer's 4-parameter limit.
/// </summary>
public sealed class ProtobufSchemaAwarePrettyPrintContext
{
    /// <summary>
    ///     Gets the destination string builder.
    /// </summary>
    public required StringBuilder Builder { get; init; }

    /// <summary>
    ///     Gets the descriptor index used to resolve nested message and enum types.
    /// </summary>
    public required ProtobufDescriptorIndex Index { get; init; }
}
