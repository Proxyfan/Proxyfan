using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Writes Proxyfan's CLI help/usage information.
/// </summary>
public static class CliHelpWriter
{
    private const string HelpText = """
        Proxyfan CLI - HTTP debugging proxy

        Usage:
          proxyfan-cli <command> [options]

        Commands:
          help                  Show this help text
          version               Show version information
          start [--port N]      Start the proxy server on the given port (default: 8080)
          har-summary <path>    Print a human-readable summary of a HAR file

        Options:
          --port N              TCP port for the start command (1-65535)
          --input <path>        Alternative way to specify the HAR file path

        Examples:
          proxyfan-cli start --port 8888
          proxyfan-cli har-summary capture.har

        """;

    /// <summary>
    ///     Writes the help text to the supplied writer.
    /// </summary>
    /// <param name="writer">The destination text writer.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the write has been flushed.</returns>
    public static Task WriteHelpAsync(TextWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return writer.WriteAsync(HelpText.AsMemory(), cancellationToken);
    }
}
