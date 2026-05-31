using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;

namespace Proxyfan.Client.UiAutomationTests.Infrastructure;

/// <summary>
///     Driver around <c>Add-AppxPackage</c> / <c>Remove-AppxPackage</c> /
///     <c>shell:AppsFolder\&lt;AUMID&gt;</c> that lets the FlaUI test harness
///     run each test from a brand-new fresh MSIX install. The MSIX itself is
///     built ONCE per test-process via
///     <see cref="EnsurePackageBuiltAndSigned" /> (which delegates to
///     <c>.tools/Build-MsixPackage.ps1</c>); each test then installs and
///     uninstalls the cached package.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class MsixInstaller
{
    private const string PackageName = "Proxyfan.Proxyfan";
    private const string PackageFamilyName = "Proxyfan.Proxyfan_8e6yh7g4xk75m";
    private const string AppId = "App";

    private static readonly Lock BuildLock = new();
    private static string? _cachedMsixPath;

    /// <summary>
    ///     The full AppUserModelId of the installed MSIX. Used with
    ///     <c>explorer.exe shell:AppsFolder\&lt;aumid&gt;</c> to launch.
    /// </summary>
    public static string Aumid => $"{PackageFamilyName}!{AppId}";

    /// <summary>
    ///     Builds and signs the MSIX once per test process. Subsequent calls
    ///     return the cached path. The build invokes the
    ///     <c>.tools/Build-MsixPackage.ps1</c> script which calls
    ///     <c>dotnet publish</c> + <c>MakeAppx</c>; signing uses a self-signed
    ///     dev cert that must already be installed under
    ///     <c>LocalMachine\TrustedPeople</c>.
    /// </summary>
    /// <returns>The absolute path to the signed .msix.</returns>
    public static string EnsurePackageBuiltAndSigned()
    {
        lock (BuildLock)
        {
            if (_cachedMsixPath is not null && File.Exists(_cachedMsixPath))
            {
                return _cachedMsixPath;
            }

            var repositoryRoot = LocateRepositoryRoot();
            var msixPath = Path.Combine(repositoryRoot, "artifacts", "installer", "Proxyfan-0.0.1.0-win-x64.msix");
            if (!File.Exists(msixPath))
            {
                throw new FileNotFoundException(
                    $"MSIX package not found at '{msixPath}'. Build it first via:\n" +
                    $"  pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-MsixPackage.ps1\n" +
                    $"  then sign it with signtool against your dev cert.",
                    msixPath);
            }

            _cachedMsixPath = msixPath;
            return msixPath;
        }
    }

    /// <summary>
    ///     Installs the MSIX via <c>Add-AppxPackage</c>. Throws if installation
    ///     fails (typically because the signing cert is not trusted).
    /// </summary>
    /// <param name="msixPath">The .msix to install.</param>
    public static void Install(string msixPath)
    {
        var result = RunPowerShell($@"Add-AppxPackage -Path '{msixPath}' -ErrorAction Stop");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Add-AppxPackage failed for '{msixPath}' (exit {result.ExitCode}). " +
                $"Stderr: {result.StandardError.Trim()}");
        }
    }

    /// <summary>
    ///     Uninstalls the package via <c>Remove-AppxPackage</c>. Best-effort —
    ///     swallows errors so a failing test's teardown does not mask the
    ///     original failure.
    /// </summary>
    public static void Uninstall()
    {
        RunPowerShell(
            $"$pkg = Get-AppxPackage -Name '{PackageName}' -ErrorAction SilentlyContinue;" +
            "if ($pkg) { Remove-AppxPackage -Package $pkg.PackageFullName -ErrorAction SilentlyContinue }");
    }

    /// <summary>
    ///     Launches the installed app via <c>explorer.exe shell:AppsFolder\&lt;AUMID&gt;</c>
    ///     and returns the spawned <c>Client.Desktop.exe</c> process.
    ///     Waits up to <paramref name="readyTimeout" /> for the process to appear.
    /// </summary>
    /// <param name="readyTimeout">Maximum time to wait for the process to spawn.</param>
    /// <returns>The launched process.</returns>
    public static Process LaunchAndAttach(TimeSpan readyTimeout)
    {
        var preExisting = Process.GetProcessesByName("Client.Desktop").Select(p => p.Id).ToHashSet();
        var psi = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $@"shell:AppsFolder\{Aumid}",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(psi)?.Dispose();

        var deadline = DateTime.UtcNow + readyTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var current = Process.GetProcessesByName("Client.Desktop");
            var fresh = current.FirstOrDefault(p => !preExisting.Contains(p.Id));
            if (fresh is not null)
            {
                return fresh;
            }

            foreach (var p in current)
            {
                p.Dispose();
            }
            Thread.Sleep(150);
        }

        throw new TimeoutException(
            $"Client.Desktop.exe (AUMID {Aumid}) did not start within {readyTimeout.TotalSeconds:F1}s.");
    }

    private static string LocateRepositoryRoot()
    {
        // Walk upwards from the test assembly location until we find Proxyfan.slnx
        // — the canonical repo root marker.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Proxyfan.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate Proxyfan repository root (no Proxyfan.slnx found walking upward from test assembly).");
    }

    private static ProcessResult RunPowerShell(string script)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pwsh",
            ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
