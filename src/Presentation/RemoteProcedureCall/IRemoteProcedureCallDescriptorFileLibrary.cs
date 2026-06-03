using System.Collections.Generic;

namespace Proxyfan.Presentation.RemoteProcedureCall;

/// <summary>
///     Presentation-safe abstraction for loading and unloading descriptor-set files.
/// </summary>
public interface IRemoteProcedureCallDescriptorFileLibrary
{
    /// <summary>
    ///     Gets the file paths from which descriptor sets are currently loaded.
    /// </summary>
    IReadOnlyList<string> LoadedFilePaths { get; }

    /// <summary>
    ///     Removes every loaded descriptor set.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Loads a binary <c>FileDescriptorSet</c> payload from a source path.
    /// </summary>
    /// <param name="sourcePath">A path identifying the descriptor source.</param>
    /// <param name="payload">The binary FileDescriptorSet bytes.</param>
    void Load(string sourcePath, byte[] payload);

    /// <summary>
    ///     Removes the descriptor set loaded from <paramref name="sourcePath" />, if any.
    /// </summary>
    /// <param name="sourcePath">The source path to remove.</param>
    void Unload(string sourcePath);
}
