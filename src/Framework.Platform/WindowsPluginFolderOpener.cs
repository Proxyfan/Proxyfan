using Proxyfan.Framework.Extensibility;
using System;
using System.Diagnostics;
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
            var info = new ProcessStartInfo("explorer.exe", $"\"{directoryPath}\"")
            {
                UseShellExecute = false,
            };
            using var process = Process.Start(info);
            _ = process;
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
