using Proxyfan.Framework.Serialization;
using Proxyfan.Presentation.RemoteProcedureCall;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Adapts <see cref="IRemoteProcedureCallDescriptorLibrary" /> to the presentation-safe
///     <see cref="IRemoteProcedureCallDescriptorFileLibrary" /> abstraction used by tools UI.
/// </summary>
public sealed class RemoteProcedureCallDescriptorFileLibraryAdapter : IRemoteProcedureCallDescriptorFileLibrary
{
    private readonly IRemoteProcedureCallDescriptorLibrary _library;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RemoteProcedureCallDescriptorFileLibraryAdapter" /> class.
    /// </summary>
    /// <param name="library">The underlying descriptor library.</param>
    public RemoteProcedureCallDescriptorFileLibraryAdapter(IRemoteProcedureCallDescriptorLibrary library)
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
