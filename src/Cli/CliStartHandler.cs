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
        try
        {
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return 0;
        }

        var proxyServer = host.Services.GetRequiredService<ProxyServer>();
        var startResult = await proxyServer.StartAsync(cancellationToken).ConfigureAwait(false);
        if (!startResult.IsSuccess)
        {
            var errorMessage = startResult.Error is null ? "unknown error" : startResult.Error.Message;
            await standardError.WriteAsync($"Failed to start proxy: {errorMessage}".AsMemory(), cancellationToken).ConfigureAwait(false);
            await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
            host.Dispose();
            return 4;
        }

        await standardOut.WriteAsync($"Proxy server listening on port {command.Port.ToString(CultureInfo.InvariantCulture)}. Press Ctrl+C to stop.{Environment.NewLine}".AsMemory(), cancellationToken).ConfigureAwait(false);

        await WaitForShutdownAsync(startOptions, cancellationToken).ConfigureAwait(false);

        await proxyServer.StopAsync(CancellationToken.None).ConfigureAwait(false);
        var exportArguments = new CliStartExportArguments
        {
            Host = host,
            StandardError = standardError,
            StandardOut = standardOut,
            StartOptions = startOptions,
        };
        await ExportCapturedFlowsAsync(exportArguments, CancellationToken.None).ConfigureAwait(false);
        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        host.Dispose();
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

            await arguments.StandardOut.WriteAsync($"Exported {flows.Count.ToString(CultureInfo.InvariantCulture)} flow(s) to {arguments.StartOptions.OutputPath}{Environment.NewLine}".AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            await arguments.StandardError.WriteAsync($"Failed to write HAR output: {ex.Message}".AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            await arguments.StandardError.WriteAsync($"Failed to write HAR output: {ex.Message}".AsMemory(), cancellationToken).ConfigureAwait(false);
        }
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
}
