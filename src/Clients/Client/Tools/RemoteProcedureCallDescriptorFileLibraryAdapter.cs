using Proxyfan.Framework.Serialization;
using Proxyfan.Presentation.RemoteProcedureCall;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Adapts the framework-layer <see cref="IRemoteProcedureCallDescriptorLibrary" /> to the
///     presentation-layer <see cref="IRemoteProcedureCallDescriptorFileLibrary" /> boundary so
///     that view models never reference framework serialization types directly.
/// </summary>
public sealed class RemoteProcedureCallDescriptorFileLibraryAdapter : IRemoteProcedureCallDescriptorFileLibrary
{
    private readonly IRemoteProcedureCallDescriptorLibrary _inner;

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallDescriptorFileLibraryAdapter" />
    ///     that delegates to the supplied <paramref name="inner" /> library.
    /// </summary>
    /// <param name="inner">The underlying framework descriptor library.</param>
    public RemoteProcedureCallDescriptorFileLibraryAdapter(IRemoteProcedureCallDescriptorLibrary inner)
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
