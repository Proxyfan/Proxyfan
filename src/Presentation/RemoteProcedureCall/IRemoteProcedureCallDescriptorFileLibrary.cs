using System.Collections.Generic;

namespace Proxyfan.Presentation.RemoteProcedureCall;

/// <summary>
///     Presentation-safe descriptor library abstraction used by UI tools that load
///     and unload protobuf descriptor sets for gRPC inspection.
/// </summary>
public interface IRemoteProcedureCallDescriptorFileLibrary
{
    /// <summary>
    ///     Gets the source paths of currently loaded descriptor-set files.
    /// </summary>
    IReadOnlyList<string> LoadedFilePaths { get; }

    /// <summary>
    ///     Clears all loaded descriptor files.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Loads a descriptor-set payload for the provided source path.
    /// </summary>
    /// <param name="sourcePath">A path identifying the descriptor source.</param>
    /// <param name="payload">The binary descriptor-set payload.</param>
    void Load(string sourcePath, byte[] payload);

    /// <summary>
    ///     Unloads the descriptor file associated with the provided source path.
    /// </summary>
    /// <param name="sourcePath">A path identifying the descriptor source.</param>
    void Unload(string sourcePath);
}
