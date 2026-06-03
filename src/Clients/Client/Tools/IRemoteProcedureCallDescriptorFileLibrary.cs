using System.Collections.Generic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Descriptor-file operations required by the gRPC descriptor tool UI.
/// </summary>
public interface IRemoteProcedureCallDescriptorFileLibrary
{
    /// <summary>
    ///     Gets the file paths from which descriptor sets are currently loaded.
    /// </summary>
    IReadOnlyList<string> LoadedFilePaths { get; }

    /// <summary>
    ///     Removes every loaded descriptor set and resets the library to an empty state.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Loads a binary <c>FileDescriptorSet</c> payload for a source path.
    /// </summary>
    /// <param name="sourcePath">A path identifying the descriptor source.</param>
    /// <param name="payload">The binary FileDescriptorSet bytes.</param>
    void Load(string sourcePath, byte[] payload);

    /// <summary>
    ///     Removes the descriptor set previously loaded from <paramref name="sourcePath" />.
    /// </summary>
    /// <param name="sourcePath">The source path to remove.</param>
    void Unload(string sourcePath);
}
