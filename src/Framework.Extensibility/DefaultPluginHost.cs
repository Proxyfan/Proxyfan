using Proxyfan.Plugin.Abstractions;
using System.Collections.Generic;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Default in-process <see cref="IPluginHost" /> used by the application at startup.
///     The host records every extension-point registration via internal collections so
///     that the plugin manager UI can surface what each plugin contributed. Live wiring
///     into the inspector / decoder / formatter registries is performed by adapters in the
///     presentation/framework layers reading <see cref="ContentDecoderRegistrations" />,
///     <see cref="InspectorTabRegistrations" />, and <see cref="ExportFormatterRegistrations" />.
/// </summary>
public sealed class DefaultPluginHost : IPluginHost
{
    private readonly List<PluginContentDecoderRegistration> _contentDecoderRegistrations;
    private readonly List<PluginExportFormatterRegistration> _exportFormatterRegistrations;
    private readonly List<PluginInspectorTabRegistration> _inspectorTabRegistrations;

    /// <summary>
    ///     Gets the snapshot of content decoder registrations contributed by plugins.
    /// </summary>
    public IReadOnlyList<PluginContentDecoderRegistration> ContentDecoderRegistrations => _contentDecoderRegistrations;

    /// <summary>
    ///     Gets the snapshot of export formatter registrations contributed by plugins.
    /// </summary>
    public IReadOnlyList<PluginExportFormatterRegistration> ExportFormatterRegistrations => _exportFormatterRegistrations;

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
        _exportFormatterRegistrations = [];
        _inspectorTabRegistrations = [];
    }

    /// <inheritdoc />
    public string ApiVersion { get; }

    /// <inheritdoc />
    public void RegisterContentDecoder(IContentDecoder decoder)
    {
        var registration = new PluginContentDecoderRegistration(decoder);
        _contentDecoderRegistrations.Add(registration);
    }

    /// <inheritdoc />
    public void RegisterExportFormatter(IExportFormatter formatter)
    {
        var registration = new PluginExportFormatterRegistration(formatter);
        _exportFormatterRegistrations.Add(registration);
    }

    /// <inheritdoc />
    public void RegisterInspectorTab(ITrafficInspector inspector)
    {
        var registration = new PluginInspectorTabRegistration(inspector);
        _inspectorTabRegistrations.Add(registration);
    }
}
