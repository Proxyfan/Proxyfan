using System.Collections.Generic;

namespace Proxyfan.Presentation.RemoteProcedureCall;

/// <summary>
///     Presentation-safe abstraction for the mutable set of loaded protobuf
///     <c>FileDescriptorSet</c> files used by the gRPC descriptors tool window.
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
    ///     Loads a binary <c>FileDescriptorSet</c> payload, replacing any previously loaded
    ///     content from the same source path.
    /// </summary>
    /// <param name="sourcePath">A path identifying the descriptor source.</param>
    /// <param name="payload">The binary FileDescriptorSet bytes.</param>
    void Load(string sourcePath, byte[] payload);

    /// <summary>
    ///     Removes the descriptor set previously loaded from <paramref name="sourcePath" />,
    ///     if any.
    /// </summary>
    /// <param name="sourcePath">The source path to remove.</param>
    void Unload(string sourcePath);
}
