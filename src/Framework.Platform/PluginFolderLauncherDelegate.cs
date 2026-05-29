using System;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Delegate used by <see cref="WindowsPluginFolderOpener" /> to launch an external
///     process or external-shell command that opens the supplied directory. Returns the
///     launched process wrapped as an <see cref="IDisposable" /> (so the caller can dispose
///     it deterministically) or <see langword="null" /> when the launch failed gracefully.
/// </summary>
/// <param name="directoryPath">The directory to open. Already validated to exist and to be non-empty.</param>
/// <returns>The launched process wrapper, or <see langword="null" />.</returns>
public delegate IDisposable? PluginFolderLauncherDelegate(string directoryPath);
