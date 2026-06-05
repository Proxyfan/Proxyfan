using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Integration-style tests for <see cref="CliStartHandler" />. These exercise the
///     end-to-end headless proxy bootstrap and shutdown without driving real traffic
///     through the listener.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CliStartHandlerTests
{
    /// <summary>
    ///     When the supplied cancellation token is already cancelled before the handler runs,
    ///     the host startup is short-circuited and the handler returns 0.
    /// </summary>
    [Test]
    public async Task RunAsync_WhenCancellationAlreadyRequested_ReturnsZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = BuildStartCommand(GetFreePort(), null, durationSeconds: null);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var exitCode = await CliStartHandler.RunAsync(command, output, error, cancellationSource.Token);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    /// <summary>
    ///     When a duration is supplied, the handler self-terminates after the elapsed time
    ///     even without an external Ctrl+C, and emits the "listening" startup message.
    /// </summary>
    [Test]
    public async Task RunAsync_WithDurationLimit_StopsAutomaticallyAndAnnouncesPort()
    {
        var port = GetFreePort();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = BuildStartCommand(port, outputPath: null, durationSeconds: 1);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var exitCode = await CliStartHandler.RunAsync(command, output, error, cancellationSource.Token);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await Assert.That(output.ToString()).Contains("listening");
    }

    /// <summary>
    ///     When <c>--output</c> is supplied, the handler writes a HAR file (even when no
    ///     traffic was captured, it produces an empty HAR document).
    /// </summary>
    [Test]
    public async Task RunAsync_WithOutputPath_WritesEmptyHarFile()
    {
        var port = GetFreePort();
        var outputPath = Path.Combine(Path.GetTempPath(), $"proxyfan-cli-test-{Guid.NewGuid():N}.har");
        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            var command = BuildStartCommand(port, outputPath, durationSeconds: 1);
            using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var exitCode = await CliStartHandler.RunAsync(command, output, error, cancellationSource.Token);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(File.Exists(outputPath)).IsTrue();
            var harContent = await File.ReadAllTextAsync(outputPath);
            await Assert.That(harContent).Contains("\"log\"");
            await Assert.That(output.ToString()).Contains("Exported");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    ///     When the bind port is already in use, the handler reports the failure on standard
    ///     error and exits with a non-zero code.
    /// </summary>
    [Test]
    public async Task RunAsync_PortAlreadyInUse_ReportsBindFailure()
    {
        var port = GetFreePort();
        var blocker = new TcpListener(IPAddress.Loopback, port);
        blocker.Start();
        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            var command = BuildStartCommand(port, outputPath: null, durationSeconds: 1);
            using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var exitCode = await CliStartHandler.RunAsync(command, output, error, cancellationSource.Token);

            await Assert.That(exitCode).IsNotEqualTo(0);
            await Assert.That(error.ToString()).Contains("Failed to start proxy");
        }
        finally
        {
            blocker.Stop();
        }
    }

    /// <summary>
    ///     Without a duration limit, the handler waits indefinitely until the cancellation
    ///     token fires, then shuts down cleanly. This exercises the no-duration branch of
    ///     <c>WaitForShutdownAsync</c>.
    /// </summary>
    [Test]
    public async Task RunAsync_NoDurationLimit_ShutsDownWhenCancellationFires()
    {
        var port = GetFreePort();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = BuildStartCommand(port, outputPath: null, durationSeconds: null);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var exitCode = await CliStartHandler.RunAsync(command, output, error, cancellationSource.Token);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("listening");
    }

    /// <summary>
    ///     If writing the startup status fails after the proxy has started, shutdown cleanup
    ///     still runs so a subsequent start can bind the same port.
    /// </summary>
    [Test]
    public async Task RunAsync_WhenListeningWriteFails_CleansUpStartedProxy()
    {
        var port = GetFreePort();
        var command = BuildStartCommand(port, outputPath: null, durationSeconds: 1);
        using var disposedOutput = new StringWriter();
        disposedOutput.Dispose();
        using var firstError = new StringWriter();
        using var firstCancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await Assert.That(() => CliStartHandler.RunAsync(command, disposedOutput, firstError, firstCancellationSource.Token))
            .Throws<ObjectDisposedException>();

        using var secondOutput = new StringWriter();
        using var secondError = new StringWriter();
        using var secondCancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var secondExitCode = await CliStartHandler.RunAsync(command, secondOutput, secondError, secondCancellationSource.Token);

        await Assert.That(secondExitCode).IsEqualTo(0);
        await Assert.That(secondOutput.ToString()).Contains("listening");
    }

    /// <summary>
    ///     When the supplied <c>--output</c> path cannot be opened for writing (here, because
    ///     it points to a directory rather than a file), the export helper catches the
    ///     <see cref="IOException" /> and reports the failure on standard error.
    /// </summary>
    [Test]
    public async Task RunAsync_OutputPathIsDirectory_ReportsExportFailure()
    {
        var port = GetFreePort();
        var outputPath = Path.GetTempPath();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = BuildStartCommand(port, outputPath, durationSeconds: 1);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var exitCode = await CliStartHandler.RunAsync(command, output, error, cancellationSource.Token);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(error.ToString()).Contains("Failed to write HAR output");
    }

    private static CliCommand BuildStartCommand(int port, string? outputPath, int? durationSeconds)
    {
        var startOptions = new CliStartOptions
        {
            DurationSeconds = durationSeconds,
            OutputPath = outputPath,
        };
        var command = new CliCommand(CliCommandKind.Start, port, null)
        {
            StartOptions = startOptions,
        };
        return command;
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.IPv6Any, 0);
        listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
