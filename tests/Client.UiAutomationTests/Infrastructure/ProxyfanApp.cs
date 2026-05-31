using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.UiAutomationTests.Infrastructure;

/// <summary>
///     Per-test orchestration of a real <c>Client.Desktop.exe</c> process driven
///     through Windows UI Automation via FlaUI. Each test owns one instance,
///     interacts with it through real mouse/keyboard automation, and disposes it
///     to terminate the process and clean up the per-test working directory.
///     <para>
///         The launched process is isolated from the developer's environment:
///         <list type="bullet">
///             <item>
///                 <c>LOCALAPPDATA</c> is redirected to a per-test temp directory so
///                 user config / certificates / logs are never read or written under
///                 the real <c>%LOCALAPPDATA%\Proxyfan</c>.
///             </item>
///             <item>
///                 <c>proxy__port</c> is set to a per-test ephemeral high port so
///                 parallel developer activity (the real Proxyfan at 8080, other
///                 dev servers) never conflicts.
///             </item>
///             <item>
///                 <c>proxy__isRegisterSystemProxy=false</c> guarantees the test
///                 process never touches the actual Windows Internet Settings
///                 system proxy registry value.
///             </item>
///             <item>
///                 <c>proxy__isAutoStart=false</c> avoids the background TCP listener
///                 entirely when the test does not need the proxy at all (overridable
///                 via constructor argument when a specific test needs traffic flowing).
///             </item>
///         </list>
///     </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ProxyfanApp : IAsyncDisposable
{
    private static int _nextPortOffset;
    private readonly UIA3Automation _automation;
    private readonly Application _application;
    private readonly string _userDataDirectory;
    private Window? _cachedMainWindow;

    /// <summary>
    ///     Path to the freshly built <c>Client.Desktop.exe</c>, computed at build
    ///     time and embedded in the test assembly via <c>AssemblyMetadata</c>.
    /// </summary>
    public static string DesktopExecutablePath { get; } = ResolveDesktopExecutablePath();

    /// <summary>
    ///     The FlaUI UIA3 automation root. Tests rarely need this directly.
    /// </summary>
    public UIA3Automation Automation => _automation;

    /// <summary>
    ///     The underlying FlaUI <see cref="Application" />. Useful for advanced
    ///     scenarios; prefer <see cref="GetMainWindow" /> for normal interactions.
    /// </summary>
    public Application Application => _application;

    /// <summary>
    ///     The temporary directory hosting the test process's
    ///     <c>%LOCALAPPDATA%</c>. Inspect after the test to verify side-effects.
    /// </summary>
    public string UserDataDirectory => _userDataDirectory;

    /// <summary>
    ///     The ephemeral proxy port assigned to this app instance.
    /// </summary>
    public int ProxyPort { get; }

    /// <summary>
    ///     Whether this app was launched via the MSIX install path (and therefore
    ///     must be uninstalled on dispose). When <see langword="false" /> the app
    ///     was started via direct .exe launch with env-var sandbox.
    /// </summary>
    public bool InstalledViaMsix { get; }

    private ProxyfanApp(Application application, UIA3Automation automation, string userDataDirectory, int port, bool installedViaMsix)
    {
        _application = application;
        _automation = automation;
        _userDataDirectory = userDataDirectory;
        ProxyPort = port;
        InstalledViaMsix = installedViaMsix;
    }

    /// <summary>
    ///     Launches a new Proxyfan desktop process under test isolation and waits
    ///     for the shell window to appear.
    /// </summary>
    /// <param name="enableProxyListener">
    ///     When <see langword="true" /> the launched process binds the TCP
    ///     proxy listener on <see cref="ProxyPort" />. Defaults to <see langword="false" />
    ///     so tests that only exercise the UI never open a network socket.
    /// </param>
    /// <param name="readyTimeout">
    ///     How long to wait for the main window to become responsive. Defaults to
    ///     30 seconds — generous enough for a cold-start JIT pass on a slow agent.
    /// </param>
    /// <returns>The launched, ready-to-drive <see cref="ProxyfanApp" />.</returns>
    public static ProxyfanApp Launch(bool enableProxyListener = false, TimeSpan? readyTimeout = null)
    {
        if (UseMsixInstall)
        {
            return LaunchViaMsix(readyTimeout ?? TimeSpan.FromSeconds(45));
        }

        return LaunchViaDirectExe(enableProxyListener, readyTimeout ?? TimeSpan.FromSeconds(30));
    }

    /// <summary>
    ///     When <see langword="true" />, every <see cref="Launch" /> call installs
    ///     the signed MSIX, launches via <c>shell:AppsFolder\&lt;AUMID&gt;</c>,
    ///     then uninstalls on dispose — exactly what an end user does on a fresh
    ///     install. Defaults to <see langword="true" />; set the
    ///     <c>PROXYFAN_UI_TESTS_SKIP_MSIX</c> environment variable to opt out and
    ///     use the faster direct-.exe path (~2 s/test vs. ~60 s/test).
    /// </summary>
    public static bool UseMsixInstall { get; } =
        !string.Equals(
            Environment.GetEnvironmentVariable("PROXYFAN_UI_TESTS_SKIP_MSIX"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static ProxyfanApp LaunchViaMsix(TimeSpan readyTimeout)
    {
        var msixPath = MsixInstaller.EnsurePackageBuiltAndSigned();
        // Make sure no stale install lingers from a prior crashed test.
        MsixInstaller.Uninstall();
        MsixInstaller.Install(msixPath);

        Process? launchedProcess = null;
        UIA3Automation? automation = null;
        Application? application = null;
        try
        {
            launchedProcess = MsixInstaller.LaunchAndAttach(readyTimeout);
            application = Application.Attach(launchedProcess.Id);
            automation = new UIA3Automation();
            var window = application.GetMainWindow(automation, readyTimeout)
                ?? throw new InvalidOperationException(
                    "Installed MSIX Client.Desktop.exe did not present a main window within the ready timeout.");
            try
            {
                window.SetForeground();
            }
            catch
            {
                // Best-effort.
            }

            // MSIX path: no temp user-data directory (the MSIX containerises
            // user data per-package), and proxy port is the one baked into
            // appsettings.json (default 8080). We pass 0 as a sentinel so
            // tests do not assert a specific port.
            var instance = new ProxyfanApp(application, automation, userDataDirectory: msixPath, port: 0, installedViaMsix: true);
            instance._cachedMainWindow = window;
            return instance;
        }
        catch
        {
            automation?.Dispose();
            try
            {
                application?.Close();
            }
            catch
            {
                // Best-effort.
            }
            try
            {
                if (launchedProcess is not null && !launchedProcess.HasExited)
                {
                    launchedProcess.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort.
            }

            MsixInstaller.Uninstall();
            throw;
        }
    }

    private static ProxyfanApp LaunchViaDirectExe(bool enableProxyListener, TimeSpan readyTimeout)
    {
        var port = AllocateEphemeralPort();
        var userDataDirectory = Path.Combine(
            Path.GetTempPath(),
            "Proxyfan.UiTests",
            "user-data-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(userDataDirectory);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = DesktopExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(DesktopExecutablePath)!,
            UseShellExecute = false,
            CreateNoWindow = false,
        };
        processStartInfo.Environment["LOCALAPPDATA"] = userDataDirectory;
        processStartInfo.Environment["proxy__port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        processStartInfo.Environment["proxy__isAutoStart"] = enableProxyListener ? "true" : "false";
        processStartInfo.Environment["proxy__isRegisterSystemProxy"] = "false";
        processStartInfo.Environment["updates__isEnabled"] = "false";

        var application = Application.Launch(processStartInfo);
        UIA3Automation? automation = null;
        try
        {
            automation = new UIA3Automation();
            var window = application.GetMainWindow(automation, readyTimeout)
                ?? throw new InvalidOperationException("Client.Desktop.exe did not present a main window within the ready timeout.");
            try
            {
                window.SetForeground();
            }
            catch
            {
                // Best-effort.
            }

            var instance = new ProxyfanApp(application, automation, userDataDirectory, port, installedViaMsix: false);
            instance._cachedMainWindow = window;
            return instance;
        }
        catch
        {
            automation?.Dispose();
            try
            {
                application.Close();
            }
            catch
            {
                // Best-effort cleanup on failed launch.
            }

            try
            {
                Directory.Delete(userDataDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }

            throw;
        }
    }

    /// <summary>
    ///     Returns the live main window. Re-queries when necessary so a window
    ///     activation does not stale the cached reference.
    /// </summary>
    /// <returns>The shell window.</returns>
    public Window GetMainWindow()
    {
        if (_cachedMainWindow is not null && _cachedMainWindow.Properties.IsEnabled.IsSupported)
        {
            return _cachedMainWindow;
        }

        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("Main window is no longer accessible.");
        _cachedMainWindow = window;
        return window;
    }

    /// <summary>
    ///     Polls the desktop for a top-level <see cref="Window" /> owned by this
    ///     app's process whose title matches <paramref name="title" /> exactly.
    ///     Used to assert that a tool window opened in response to a UI gesture.
    /// </summary>
    /// <param name="title">The exact window title to match.</param>
    /// <param name="timeout">Optional upper bound; defaults to 15 seconds.</param>
    /// <returns>The matching window.</returns>
    /// <exception cref="TimeoutException">When the window does not appear in time.</exception>
    public Window WaitForToolWindow(string title, TimeSpan? timeout = null)
    {
        var effective = timeout ?? TimeSpan.FromSeconds(15);
        var deadline = DateTime.UtcNow + effective;
        var processId = GetMainWindow().Properties.ProcessId.Value;

        Window? result = null;
        var lastSeenNames = new List<string>();
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var desktop = _automation.GetDesktop();
                // Avalonia tool windows opened via Window.Show(owner) may
                // appear as descendants (owned popup) rather than direct
                // children of the desktop, so traverse the full descendant
                // tree scoped by process id.
                var windows = desktop.FindAllDescendants(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                      .And(cf.ByProcessId(processId)));
                lastSeenNames.Clear();
                foreach (var raw in windows)
                {
                    var rawName = raw.Name ?? string.Empty;
                    lastSeenNames.Add(rawName);
                    if (string.Equals(rawName, title, StringComparison.Ordinal))
                    {
                        result = raw.AsWindow();
                        break;
                    }
                }
            }
            catch
            {
                // Best-effort while window list is changing under us.
            }

            if (result is not null)
            {
                return result;
            }

            Thread.Sleep(150);
        }

        var seenList = lastSeenNames.Count == 0
            ? "(no child windows visible)"
            : string.Join(", ", lastSeenNames.Select(n => $"'{n}'"));
        throw new TimeoutException(
            $"Timed out after {effective.TotalSeconds:F1}s waiting for tool window titled '{title}'. Last seen child windows: {seenList}.");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // FlaUI's Application.Close() asks the process to close gracefully via
        // CloseMainWindow, waits 5 seconds, kills it if necessary, then disposes
        // the Process object. After this call the application is gone and any
        // further property access (HasExited included) throws because the Process
        // wrapper has been released. So we only ever call Close() once here.
        try
        {
            _application.Close();
        }
        catch
        {
            // Best-effort; if Close fails (e.g. process already dead), the
            // Application.Dispose() below still releases automation handles.
        }

        try
        {
            _automation.Dispose();
        }
        catch
        {
            // Best-effort.
        }

        try
        {
            _application.Dispose();
        }
        catch
        {
            // Best-effort.
        }

        if (InstalledViaMsix)
        {
            // Uninstall the MSIX so the next test starts from a true fresh
            // install. Best-effort; the install step also pre-uninstalls.
            try
            {
                MsixInstaller.Uninstall();
            }
            catch
            {
                // Best-effort.
            }
        }
        else
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (Directory.Exists(_userDataDirectory))
                    {
                        Directory.Delete(_userDataDirectory, recursive: true);
                    }

                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(200);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(200);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    private static int AllocateEphemeralPort()
    {
        var offset = Interlocked.Increment(ref _nextPortOffset);
        // Per-test counter guarantees uniqueness within one test run; the OS verifies
        // availability when the listener actually binds. Range chosen to dodge common
        // dev-server ports.
        var candidate = 50800 + (offset % 4000);
        // Sanity check the port is free; if not, scan forward.
        for (var i = 0; i < 200; i++)
        {
            if (IsPortFree(candidate))
            {
                return candidate;
            }

            candidate++;
        }

        throw new InvalidOperationException("No free ephemeral port available for the test process.");
    }

    private static bool IsPortFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static string ResolveDesktopExecutablePath()
    {
        var assembly = typeof(ProxyfanApp).Assembly;
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        var pathAttribute = FindPathAttribute(metadata)
            ?? throw new InvalidOperationException(
                "Assembly metadata 'ProxyfanDesktopExePath' is missing; rebuild Client.UiAutomationTests.");

        var path = pathAttribute.Value
            ?? throw new InvalidOperationException("ProxyfanDesktopExePath assembly metadata is empty.");
        var canonical = Path.GetFullPath(path);
        if (!File.Exists(canonical))
        {
            throw new FileNotFoundException(
                $"Client.Desktop.exe not found at the path embedded by MSBuild: '{canonical}'. Build the desktop project first.",
                canonical);
        }

        return canonical;
    }

    private static AssemblyMetadataAttribute? FindPathAttribute(IEnumerable<AssemblyMetadataAttribute> metadata)
    {
        foreach (var attribute in metadata)
        {
            if (string.Equals(attribute.Key, "ProxyfanDesktopExePath", StringComparison.Ordinal))
            {
                return attribute;
            }
        }

        return null;
    }
}
