using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     In-memory implementation of <see cref="IPluginHost" /> used by tests and by the live
///     plugin loader. Each registration is recorded and queryable.
/// </summary>
public sealed class RecordingPluginHost : IPluginHost
{
    private readonly List<string> _contentDecoders;
    private readonly List<string> _inspectorTabs;

    /// <summary>
    ///     Gets the list of registered content-type patterns with their decoder names, in
    ///     registration order. Each entry is formatted "pattern => decoderName".
    /// </summary>
    public IReadOnlyList<string> ContentDecoders => _contentDecoders;

    /// <summary>
    ///     Gets the list of registered inspector tab names in registration order.
    /// </summary>
    public IReadOnlyList<string> InspectorTabs => _inspectorTabs;

    /// <summary>
    ///     Initializes a new <see cref="RecordingPluginHost" /> with the supplied API version.
    /// </summary>
    /// <param name="apiVersion">The host API version (SemVer).</param>
    public RecordingPluginHost(string apiVersion)
    {
        ApiVersion = apiVersion;
        var contentDecoders = new List<string>();
        var inspectorTabs = new List<string>();
        _contentDecoders = contentDecoders;
        _inspectorTabs = inspectorTabs;
    }

    /// <inheritdoc />
    public string ApiVersion { get; }

    /// <inheritdoc />
    public void RegisterContentDecoder(string contentTypePattern, string decoderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypePattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(decoderName);
        _contentDecoders.Add($"{contentTypePattern} => {decoderName}");
    }

    /// <inheritdoc />
    public void RegisterInspectorTab(string tabName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tabName);
        _inspectorTabs.Add(tabName);
    }
}
