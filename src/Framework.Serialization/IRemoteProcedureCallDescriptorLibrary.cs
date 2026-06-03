using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     A live, in-memory registry of loaded protobuf <c>FileDescriptorSet</c> files. The
///     library is the inspector-facing surface that the gRPC payload formatter consults
///     when rendering payloads with named fields. Implementations are thread-safe.
/// </summary>
public interface IRemoteProcedureCallDescriptorLibrary
{
    /// <summary>
    ///     Gets the merged descriptor index covering every loaded file.
    /// </summary>
    ProtobufDescriptorIndex Index { get; }

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
    /// <param name="sourcePath">A path identifying the descriptor source (file path or URI).</param>
    /// <param name="payload">The binary FileDescriptorSet bytes.</param>
    void Load(string sourcePath, ReadOnlyMemory<byte> payload);

    /// <summary>
    ///     Removes the descriptor set previously loaded from <paramref name="sourcePath" />,
    ///     if any. No-op when the path was not previously loaded.
    /// </summary>
    /// <param name="sourcePath">The source path to remove.</param>
    void Unload(string sourcePath);
}