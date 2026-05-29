using Proxyfan.Framework.Extensibility;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Windows implementation of <see cref="IPluginFolderOpener" />. Launches the supplied
///     directory in Windows Explorer using <c>explorer.exe</c>. Failures are swallowed —
///     UI command handlers MUST NOT raise exceptions to the user.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPluginFolderOpener : IPluginFolderOpener
{
    private readonly PluginFolderLauncherDelegate _launcher;

    /// <summary>
    ///     Initializes a new <see cref="WindowsPluginFolderOpener" /> that launches Windows
    ///     Explorer via <c>explorer.exe</c>.
    /// </summary>
    public WindowsPluginFolderOpener()
        : this(ExplorerPluginFolderLauncher.Launch)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="WindowsPluginFolderOpener" /> with a custom process
    ///     launcher. Intended for unit testing; production callers should use the parameterless
    ///     constructor that launches Windows Explorer.
    /// </summary>
    /// <param name="launcher">
    ///     A callback invoked with the validated directory path.
    /// </param>
    public WindowsPluginFolderOpener(PluginFolderLauncherDelegate launcher)
    {
        _launcher = launcher;
    }

    /// <inheritdoc />
    public void Open(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            var process = _launcher(directoryPath);
            process?.Dispose();
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
