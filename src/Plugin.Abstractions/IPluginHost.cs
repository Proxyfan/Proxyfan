namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     The host API surface exposed to plugins. Plugins receive this object via
///     <see cref="IProxyfanPlugin.Initialize" /> and use it to register their extension points
///     (inspector tabs, content decoders, export formatters).
/// </summary>
public interface IPluginHost
{
    /// <summary>
    ///     Gets the host's plugin API version (SemVer). Plugins compare this against their
    ///     declared required version in <see cref="PluginMetadata.ApiVersion" />.
    /// </summary>
    string ApiVersion { get; }

    /// <summary>
    ///     Registers a custom content decoder with the host.
    /// </summary>
    /// <param name="decoder">The decoder implementation to register.</param>
    void RegisterContentDecoder(IContentDecoder decoder);

    /// <summary>
    ///     Registers a custom export formatter with the host.
    /// </summary>
    /// <param name="formatter">The export formatter implementation to register.</param>
    void RegisterExportFormatter(IExportFormatter formatter);

    /// <summary>
    ///     Registers a custom inspector tab to be shown in the request/response inspector panel.
    /// </summary>
    /// <param name="inspector">The inspector implementation to register.</param>
    void RegisterInspectorTab(ITrafficInspector inspector);
}
