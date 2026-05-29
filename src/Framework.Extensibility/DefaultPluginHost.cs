using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Default in-process <see cref="IPluginHost" /> used by the application at startup.
///     The host records every extension-point registration via internal collections so
///     that the plugin manager UI can surface what each plugin contributed. Live wiring
///     into the inspector / decoder / column / rule registries is performed by adapters
///     in the presentation/framework layers reading <see cref="ContentDecoderRegistrations" />
///     and <see cref="InspectorTabRegistrations" />.
/// </summary>
public sealed class DefaultPluginHost : IPluginHost
{
    private readonly List<PluginContentDecoderRegistration> _contentDecoderRegistrations;
    private readonly List<PluginInspectorTabRegistration> _inspectorTabRegistrations;

    /// <summary>
    ///     Gets the snapshot of content decoder registrations contributed by plugins.
    /// </summary>
    public IReadOnlyList<PluginContentDecoderRegistration> ContentDecoderRegistrations => _contentDecoderRegistrations;

    /// <summary>
    ///     Gets the snapshot of inspector tab registrations contributed by plugins.
    /// </summary>
    public IReadOnlyList<PluginInspectorTabRegistration> InspectorTabRegistrations => _inspectorTabRegistrations;

    /// <summary>
    ///     Initializes a new <see cref="DefaultPluginHost" /> with the host's published API version.
    /// </summary>
    public DefaultPluginHost()
    {
        ApiVersion = PluginHostApiVersion.Current;
        _contentDecoderRegistrations = [];
        _inspectorTabRegistrations = [];
    }

    /// <inheritdoc />
    public string ApiVersion { get; }

    /// <inheritdoc />
    public void RegisterContentDecoder(string contentTypePattern, string decoderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypePattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(decoderName);
        var registration = new PluginContentDecoderRegistration(contentTypePattern, decoderName);
        _contentDecoderRegistrations.Add(registration);
    }

    /// <inheritdoc />
    public void RegisterInspectorTab(string tabName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tabName);
        var registration = new PluginInspectorTabRegistration(tabName);
        _inspectorTabRegistrations.Add(registration);
    }
}
