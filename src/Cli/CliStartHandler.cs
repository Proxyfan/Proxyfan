using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proxyfan.DependencyInjection;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Handles the <see cref="CliCommandKind.Start" /> command — boots a minimal headless
///     host with the proxy listener, starts the proxy server on the requested port, waits
///     for either the cancellation token or an optional auto-stop duration to fire, and
///     optionally exports captured flows to a HAR file on shutdown.
/// </summary>
[SupportedOSPlatform("windows")]
public static class CliStartHandler
{
    /// <summary>
    ///     Runs the <see cref="CliCommandKind.Start" /> command. Returns 0 on graceful
    ///     shutdown, non-zero on start failure or unexpected error.
    /// </summary>
    /// <param name="command">The parsed Start command.</param>
    /// <param name="standardOut">Standard output writer for status messages.</param>
    /// <param name="standardError">Standard error writer for failures.</param>
    /// <param name="cancellationToken">A token that triggers graceful shutdown.</param>
    /// <returns>The process exit code (0 on success).</returns>
    public static async Task<int> RunAsync(
        CliCommand command,
        TextWriter standardOut,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        var fallbackStartOptions = new CliStartOptions();
        var startOptions = command.StartOptions ?? fallbackStartOptions;
        var host = BuildHost(command.Port, startOptions.BindAddress);
        var wasCancelled = await TryStartHostAsync(host, cancellationToken).ConfigureAwait(false);
        if (wasCancelled)
        {
            host.Dispose();
            return 0;
        }

        var proxyServer = host.Services.GetRequiredService<ProxyServer>();
        var executionArguments = new StartExecutionArguments
        {
            Command = command,
            Host = host,
            ProxyServer = proxyServer,
            StandardError = standardError,
            StandardOut = standardOut,
            StartOptions = startOptions,
        };
        var startExitCode = await TryStartProxyAsync(executionArguments, cancellationToken).ConfigureAwait(false);
        if (startExitCode.HasValue)
        {
            return startExitCode.Value;
        }

        await WriteListeningAsync(executionArguments, cancellationToken).ConfigureAwait(false);
        await WaitForShutdownAsync(startOptions, cancellationToken).ConfigureAwait(false);
        await ShutdownAsync(executionArguments, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static IHost BuildHost(int port, string? bindAddress)
    {
        var hostBuilder = Host.CreateDefaultBuilder();
        hostBuilder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var inMemory = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["proxy:Port"] = port.ToString(CultureInfo.InvariantCulture),
                ["proxy:IsAutoStart"] = "false",
                ["proxy:IsRegisterSystemProxy"] = "false",
            };
            if (!string.IsNullOrWhiteSpace(bindAddress))
            {
                inMemory["proxy:BindAddress"] = bindAddress;
            }

            configurationBuilder.AddInMemoryCollection(inMemory);
        });
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IDomainEventBus, DomainEventBus>();
            services.AddProxyListener(context.Configuration);
            services.AddSingleton<ProxyServer>();
            services.AddSingleton<IHarExporter, HarExporter>();
        });
        var host = hostBuilder.Build();
        return host;
    }

    private static async Task ExportCapturedFlowsAsync(CliStartExportArguments arguments, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(arguments.StartOptions.OutputPath))
        {
            return;
        }

        try
        {
            var trafficStore = arguments.Host.Services.GetRequiredService<ITrafficStore>();
            var harExporter = arguments.Host.Services.GetRequiredService<IHarExporter>();
            var flows = trafficStore.GetAll();
            var fileStream = new FileStream(arguments.StartOptions.OutputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (fileStream.ConfigureAwait(false))
            {
                await harExporter.ExportAsync(flows, fileStream, cancellationToken).ConfigureAwait(false);
            }

            if (arguments.IsJsonOutput)
            {
                var payload = new
                {
                    status = "exported",
                    flowCount = flows.Count,
                    path = arguments.StartOptions.OutputPath,
                };
                await CliJsonWriter.WriteLineAsync(
                        arguments.StandardOut,
                        payload,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await arguments.StandardOut.WriteAsync($"Exported {flows.Count.ToString(CultureInfo.InvariantCulture)} flow(s) to {arguments.StartOptions.OutputPath}{Environment.NewLine}".AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (IOException ex)
        {
            await WriteExportErrorAsync(arguments, ex.Message, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteExportErrorAsync(arguments, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ShutdownAsync(StartExecutionArguments arguments, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        await arguments.ProxyServer.StopAsync(CancellationToken.None).ConfigureAwait(false);
        var exportArguments = new CliStartExportArguments
        {
            Host = arguments.Host,
            IsJsonOutput = arguments.Command.IsJsonOutput,
            StandardError = arguments.StandardError,
            StandardOut = arguments.StandardOut,
            StartOptions = arguments.StartOptions,
        };
        await ExportCapturedFlowsAsync(exportArguments, CancellationToken.None).ConfigureAwait(false);
        await arguments.Host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        arguments.Host.Dispose();
    }

    private static async Task<bool> TryStartHostAsync(IHost host, CancellationToken cancellationToken)
    {
        try
        {
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }

    private static async Task<int?> TryStartProxyAsync(StartExecutionArguments arguments, CancellationToken cancellationToken)
    {
        var startResult = await arguments.ProxyServer.StartAsync(cancellationToken).ConfigureAwait(false);
        if (startResult.IsSuccess)
        {
            return null;
        }

        var errorMessage = startResult.Error is null ? "unknown error" : startResult.Error.Message;
        await WriteStartFailureAsync(arguments, errorMessage, cancellationToken).ConfigureAwait(false);
        await arguments.Host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        arguments.Host.Dispose();
        return 4;
    }

    private static async Task WaitForShutdownAsync(CliStartOptions startOptions, CancellationToken cancellationToken)
    {
        if (startOptions.DurationSeconds.HasValue)
        {
            var durationSource = new CancellationTokenSource(TimeSpan.FromSeconds(startOptions.DurationSeconds.Value));
            using (durationSource)
            {
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationSource.Token);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, combined.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _ = startOptions;
                }
            }

            return;
        }

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _ = cancellationToken;
        }
    }

    private static Task WriteExportErrorAsync(
        CliStartExportArguments arguments,
        string message,
        CancellationToken cancellationToken)
    {
        if (arguments.IsJsonOutput)
        {
            var payload = new
            {
                status = "error",
                error = $"Failed to write HAR output: {message}",
            };
            return CliJsonWriter.WriteLineAsync(
                arguments.StandardError,
                payload,
                cancellationToken);
        }

        return arguments.StandardError.WriteAsync($"Failed to write HAR output: {message}".AsMemory(), cancellationToken);
    }

    private static Task WriteListeningAsync(StartExecutionArguments arguments, CancellationToken cancellationToken)
    {
        if (!arguments.Command.IsJsonOutput)
        {
            return arguments.StandardOut.WriteAsync(
                $"Proxy server listening on port {arguments.Command.Port.ToString(CultureInfo.InvariantCulture)}. Press Ctrl+C to stop.{Environment.NewLine}".AsMemory(),
                cancellationToken);
        }

        var payload = new
        {
            status = "listening",
            port = arguments.Command.Port,
        };
        return CliJsonWriter.WriteLineAsync(arguments.StandardOut, payload, cancellationToken);
    }

    private static Task WriteStartFailureAsync(
        StartExecutionArguments arguments,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        if (!arguments.Command.IsJsonOutput)
        {
            return arguments.StandardError.WriteAsync($"Failed to start proxy: {errorMessage}".AsMemory(), cancellationToken);
        }

        var payload = new
        {
            exitCode = 4,
            status = "error",
            error = $"Failed to start proxy: {errorMessage}",
        };
        return CliJsonWriter.WriteLineAsync(arguments.StandardError, payload, cancellationToken);
    }

    /// <summary>
    ///     Parameter object for the running start-command session so helper methods can stay
    ///     under the repository's parameter-count limit.
    /// </summary>
    private sealed class StartExecutionArguments
    {
        public required CliCommand Command { get; init; }

        /// <summary>
        ///     Gets the host that owns the proxy and supporting services.
        /// </summary>
        public required IHost Host { get; init; }

        /// <summary>
        ///     Gets the running proxy server instance.
        /// </summary>
        public required ProxyServer ProxyServer { get; init; }

        /// <summary>
        ///     Gets the standard-error writer for failures and diagnostic events.
        /// </summary>
        public required TextWriter StandardError { get; init; }

        /// <summary>
        ///     Gets the standard-output writer for primary command output.
        /// </summary>
        public required TextWriter StandardOut { get; init; }

        /// <summary>
        ///     Gets the parsed start options for the current invocation.
        /// </summary>
        public required CliStartOptions StartOptions { get; init; }
    }
}
