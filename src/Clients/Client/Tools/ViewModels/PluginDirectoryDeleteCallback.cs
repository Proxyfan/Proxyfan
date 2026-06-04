namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Delegate invoked by <see cref="PluginItemViewModel" /> when deleting a plugin source
///     directory from disk.
/// </summary>
/// <param name="directoryPath">The directory path to delete.</param>
/// <param name="recursive">True to delete subdirectories and files.</param>
public delegate void PluginDirectoryDeleteCallback(string directoryPath, bool recursive);
