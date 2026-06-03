using Proxyfan.Presentation.RemoteProcedureCall;
using FrameworkDescriptorLibrary = Proxyfan.Framework.Serialization.IRemoteProcedureCallDescriptorLibrary;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Adapts the framework descriptor library contract to a presentation-safe contract.
/// </summary>
public sealed class RemoteProcedureCallDescriptorFileLibraryAdapter : IRemoteProcedureCallDescriptorFileLibrary
{
    private readonly FrameworkDescriptorLibrary _inner;

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallDescriptorFileLibraryAdapter" />.
    /// </summary>
    /// <param name="inner">The framework descriptor library.</param>
    public RemoteProcedureCallDescriptorFileLibraryAdapter(FrameworkDescriptorLibrary inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _inner.Clear();
    }

    /// <inheritdoc />
    public void Load(string sourcePath, byte[] payload)
    {
        _inner.Load(sourcePath, payload);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> LoadedFilePaths => _inner.LoadedFilePaths;

    /// <inheritdoc />
    public void Unload(string sourcePath)
    {
        _inner.Unload(sourcePath);
    }
}
