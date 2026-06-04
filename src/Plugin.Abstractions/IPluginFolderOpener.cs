namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Opens a directory on disk in the host operating system's file manager (e.g. Windows
///     Explorer). Used by the Plugin Manager UI to launch a plugin's source directory so
///     that users can inspect or edit the manifest and assemblies in place.
/// </summary>
public interface IPluginFolderOpener
{
    /// <summary>
    ///     Opens <paramref name="directoryPath" /> in the host file manager. Returns silently
    ///     when the path does not exist or when the file manager cannot be launched; the
    ///     implementation MUST NOT throw, as this is invoked from UI command handlers.
    /// </summary>
    /// <param name="directoryPath">The absolute path of the directory to open.</param>
    void Open(string directoryPath);
}
