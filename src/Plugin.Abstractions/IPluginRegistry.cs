using System.Collections.Generic;

namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Read-only plugin registry surface for consumers outside framework implementation code.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    ///     Gets a detached snapshot of currently registered plugins.
    /// </summary>
    IReadOnlyList<IPluginLoadState> Plugins { get; }
}
