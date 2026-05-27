namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     The host API surface exposed to plugins. Plugins receive this object via
///     <see cref="IProxyfanPlugin.Initialize" /> and use it to register their extension points
///     (inspector tabs, content decoders, columns, rules).
/// </summary>
public interface IPluginHost
{
    /// <summary>
    ///     Gets the host's plugin API version (SemVer). Plugins compare this against their
    ///     declared required version in <see cref="PluginMetadata.ApiVersion" />.
    /// </summary>
    string ApiVersion { get; }

    /// <summary>
    ///     Registers a custom content decoder for a particular content-type (or subtype).
    /// </summary>
    /// <param name="contentTypePattern">The content-type pattern (e.g. <c>"application/json"</c>).</param>
    /// <param name="decoderName">A short human-readable decoder name shown in the UI.</param>
    void RegisterContentDecoder(string contentTypePattern, string decoderName);

    /// <summary>
    ///     Registers a custom inspector tab to be shown in the request/response inspector panel.
    /// </summary>
    /// <param name="tabName">The tab display name.</param>
    void RegisterInspectorTab(string tabName);
}
