using Proxyfan.Plugin.Abstractions;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Records a single inspector tab registration contributed by a plugin via
///     <see cref="Proxyfan.Plugin.Abstractions.IPluginHost.RegisterInspectorTab" />.
/// </summary>
public sealed class PluginInspectorTabRegistration
{
    /// <summary>
    ///     Gets the inspector implementation contributed by the plugin.
    /// </summary>
    public ITrafficInspector Inspector { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginInspectorTabRegistration" />.
    /// </summary>
    /// <param name="inspector">The inspector implementation.</param>
    public PluginInspectorTabRegistration(ITrafficInspector inspector)
    {
        Inspector = inspector;
    }
}
