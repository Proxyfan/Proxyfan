namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     The contract every Proxyfan plugin must implement. The host discovers types
///     implementing this interface in the plugin's assembly, instantiates one, reads
///     <see cref="Metadata" />, validates compatibility, and calls
///     <see cref="Initialize" /> exactly once.
/// </summary>
public interface IProxyfanPlugin
{
    /// <summary>
    ///     Gets the plugin metadata (called once before <see cref="Initialize" />).
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    ///     Initializes the plugin and registers its extension points with the host.
    /// </summary>
    /// <param name="host">The host API surface.</param>
    void Initialize(IPluginHost host);
}
