namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Delegate invoked by <see cref="PluginItemViewModel" /> whenever the user toggles the
///     enabled state or removes the plugin. The owning <see cref="PluginManagerViewModel" />
///     uses this hook to mark a restart as required and to refresh its snapshot.
/// </summary>
public delegate void PluginStateChangedCallback();
