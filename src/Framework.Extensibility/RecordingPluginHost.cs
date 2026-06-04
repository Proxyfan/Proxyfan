using Proxyfan.Plugin.Abstractions;
using System.Collections.Generic;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     In-memory implementation of <see cref="IPluginHost" /> used by tests and by the live
///     plugin loader. Each registration is recorded and queryable.
/// </summary>
public sealed class RecordingPluginHost : IPluginHost
{
    private readonly List<IContentDecoder> _contentDecoders;
    private readonly List<IExportFormatter> _exportFormatters;
    private readonly List<ITrafficInspector> _inspectorTabs;

    /// <summary>
    ///     Gets the list of registered content decoders in registration order.
    /// </summary>
    public IReadOnlyList<IContentDecoder> ContentDecoders => _contentDecoders;

    /// <summary>
    ///     Gets the list of registered export formatters in registration order.
    /// </summary>
    public IReadOnlyList<IExportFormatter> ExportFormatters => _exportFormatters;

    /// <summary>
    ///     Gets the list of registered inspector tabs in registration order.
    /// </summary>
    public IReadOnlyList<ITrafficInspector> InspectorTabs => _inspectorTabs;

    /// <summary>
    ///     Initializes a new <see cref="RecordingPluginHost" /> with the supplied API version.
    /// </summary>
    /// <param name="apiVersion">The host API version (SemVer).</param>
    public RecordingPluginHost(string apiVersion)
    {
        ApiVersion = apiVersion;
        _contentDecoders = [];
        _exportFormatters = [];
        _inspectorTabs = [];
    }

    /// <inheritdoc />
    public string ApiVersion { get; }

    /// <inheritdoc />
    public void RegisterContentDecoder(IContentDecoder decoder)
    {
        _contentDecoders.Add(decoder);
    }

    /// <inheritdoc />
    public void RegisterExportFormatter(IExportFormatter formatter)
    {
        _exportFormatters.Add(formatter);
    }

    /// <inheritdoc />
    public void RegisterInspectorTab(ITrafficInspector inspector)
    {
        _inspectorTabs.Add(inspector);
    }
}
