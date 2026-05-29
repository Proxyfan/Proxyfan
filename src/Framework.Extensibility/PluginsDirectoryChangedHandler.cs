namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Notification raised by <see cref="IPluginDirectoryWatcher" /> when the plugins root
///     directory changes (subdirectory created, deleted, or renamed). Receivers should
///     prompt the user to reload or trigger an automatic rescan.
/// </summary>
public delegate void PluginsDirectoryChangedHandler();
