using Proxyfan.Presentation.RemoteProcedureCall;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Adapts the framework descriptor library contract to a presentation-safe contract.
/// </summary>
public sealed class RemoteProcedureCallDescriptorFileLibraryAdapter : IRemoteProcedureCallDescriptorFileLibrary
{
    private readonly Proxyfan.Framework.Serialization.IRemoteProcedureCallDescriptorLibrary _library;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RemoteProcedureCallDescriptorFileLibraryAdapter" /> class.
    /// </summary>
    /// <param name="library">The framework descriptor library.</param>
    public RemoteProcedureCallDescriptorFileLibraryAdapter(Proxyfan.Framework.Serialization.IRemoteProcedureCallDescriptorLibrary library)
    {
        _library = library;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _library.Clear();
    }

    /// <inheritdoc />
    public void Load(string sourcePath, byte[] payload)
    {
        _library.Load(sourcePath, payload);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> LoadedFilePaths => _library.LoadedFilePaths;

    /// <inheritdoc />
    public void Unload(string sourcePath)
    {
        _library.Unload(sourcePath);
    }
}
