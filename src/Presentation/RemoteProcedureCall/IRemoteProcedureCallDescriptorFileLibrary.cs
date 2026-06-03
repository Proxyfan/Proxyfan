using System.Collections.Generic;

namespace Proxyfan.Presentation.RemoteProcedureCall;

/// <summary>
///     Presentation-safe contract for loading and unloading protobuf descriptor-set files used
///     by the gRPC tooling UI.
/// </summary>
public interface IRemoteProcedureCallDescriptorFileLibrary
{
    /// <summary>
    ///     Gets the source paths of the currently loaded descriptor-set files.
    /// </summary>
    IReadOnlyList<string> LoadedFilePaths { get; }

    /// <summary>
    ///     Removes every loaded descriptor set and resets the library to an empty state.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Loads a binary descriptor-set payload for a source path.
    /// </summary>
    /// <param name="sourcePath">A path identifying the descriptor source.</param>
    /// <param name="payload">The raw <c>FileDescriptorSet</c> bytes.</param>
    void Load(string sourcePath, byte[] payload);

    /// <summary>
    ///     Removes the descriptor set identified by <paramref name="sourcePath" />.
    /// </summary>
    /// <param name="sourcePath">The descriptor source path to remove.</param>
    void Unload(string sourcePath);
}
