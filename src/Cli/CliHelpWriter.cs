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
            [--bind-address IP]   and optionally override the bind address (default: 127.0.0.1)
            [--output <path>]     and optionally export captured flows to a HAR file when
            [--duration N]        the proxy stops, or auto-stop after N seconds
            [--json]              and emit newline-delimited JSON status events
          har-summary <path>    Print a human-readable summary of a HAR file
          har-to-curl <path>    Print a curl command for every request in a HAR file
          har-filter            Filter a HAR file by URL pattern, writing matching entries
            --input <path>        to a new HAR file. Useful in CI/CD pipelines.
            --output <path>
            --pattern <glob>
          har-stats <path>      Print aggregated statistics for a HAR file (status
                                distribution, methods, body bytes, duration min/median/max)
            [--json]              or emit the same metrics as a single JSON object
          send --url <url>      Print a composed HTTP/1.1 request to stdout
            [--method M]
            [--header "K: V"]
            [--body TEXT]

        Options:
          --port N              TCP port for the start command (1-65535)
          --bind-address IP      Listener bind address for start (e.g. 127.0.0.1, 0.0.0.0, ::1)
          --input <path>        Alternative way to specify the HAR file path
          --output <path>       Output HAR file path (har-filter, start)
          --duration N          Auto-stop the proxy after N seconds (start)
          --json                Emit machine-readable JSON output (start, har-stats)
          --pattern <glob>      Wildcard URL pattern (har-filter, e.g. "*.example.com/api/*")
          --method M            HTTP method (default: GET)
          --url URL             Target URL (required for send)
          --header "K: V"       Add a header (repeatable)
          --body TEXT           Request body text

        Examples:
          proxyfan-cli start --port 8888
          proxyfan-cli start --port 8888 --bind-address 127.0.0.1
          proxyfan-cli start --port 8888 --output capture.har --duration 60 --json
          proxyfan-cli har-summary capture.har
          proxyfan-cli har-stats capture.har --json
          proxyfan-cli har-filter --input capture.har --output api.har --pattern "*.example.com/api/*"
          proxyfan-cli send --method POST --url https://api.example.com --header "Accept: application/json" --body "hello"

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
