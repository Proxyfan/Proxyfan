using Proxyfan.Framework.Serialization;
using Proxyfan.Presentation.RemoteProcedureCalls;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Adapts the framework descriptor library behind a presentation-safe catalog contract.
/// </summary>
public sealed class RemoteProcedureCallDescriptorCatalog : IRemoteProcedureCallDescriptorCatalog
{
    private readonly IRemoteProcedureCallDescriptorLibrary _library;

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallDescriptorCatalog" />.
    /// </summary>
    /// <param name="library">The framework descriptor library to wrap.</param>
    public RemoteProcedureCallDescriptorCatalog(IRemoteProcedureCallDescriptorLibrary library)
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
