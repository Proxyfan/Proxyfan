using Proxyfan.Presentation.RemoteProcedureCall;
using FrameworkDescriptorLibrary = Proxyfan.Framework.Serialization.IRemoteProcedureCallDescriptorLibrary;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Adapts the framework descriptor library contract to a presentation-safe contract.
/// </summary>
public sealed class RemoteProcedureCallDescriptorFileLibraryAdapter : IRemoteProcedureCallDescriptorFileLibrary
{
    private readonly FrameworkDescriptorLibrary _library;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RemoteProcedureCallDescriptorFileLibraryAdapter" /> class.
    /// </summary>
    /// <param name="library">The framework descriptor library.</param>
    public RemoteProcedureCallDescriptorFileLibraryAdapter(FrameworkDescriptorLibrary library)
    {
        _library = library;
    }

    /// <summary>
    ///     Removes every loaded descriptor set and resets the library to an empty state.
    /// </summary>
    public void Clear()
    {
        _library.Clear();
    }

    /// <summary>
    ///     Loads a binary <c>FileDescriptorSet</c> payload for a source path.
    /// </summary>
    /// <param name="sourcePath">A path identifying the descriptor source.</param>
    /// <param name="payload">The binary FileDescriptorSet bytes.</param>
    public void Load(string sourcePath, byte[] payload)
    {
        _library.Load(sourcePath, payload);
    }

    /// <summary>
    ///     Gets the file paths from which descriptor sets are currently loaded.
    /// </summary>
    public IReadOnlyList<string> LoadedFilePaths => _library.LoadedFilePaths;

    /// <summary>
    ///     Removes the descriptor set previously loaded from a source path.
    /// </summary>
    /// <param name="sourcePath">The source path to remove.</param>
    public void Unload(string sourcePath)
    {
        _library.Unload(sourcePath);
    }
}
