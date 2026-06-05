using System;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliRunner" /> covering all command routes.
/// </summary>
public sealed class CliRunnerTests
{
    /// <summary>
    ///     Verifies that the Help command writes the help text and returns zero.
    /// </summary>
    [Test]
    public async Task RunAsync_HelpCommand_WritesHelpText()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.Help, 8080, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("Proxyfan");
    }

    /// <summary>
    ///     Verifies that the Version command writes a version string and returns zero.
    /// </summary>
    [Test]
    public async Task RunAsync_VersionCommand_WritesVersionString()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.Version, 8080, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("Proxyfan");
    }

    /// <summary>
    ///     Verifies that the Start command boots a real proxy listener and exits cleanly when
    ///     the cancellation token fires before startup completes.
    /// </summary>
    [Test]
    public async Task RunAsync_StartCommandCancelled_ReturnsZero()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.Start, 9999, null);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var exitCode = await runner.RunAsync(command, output, error, cancellationSource.Token);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an Unknown command writes an error message and returns non-zero.
    /// </summary>
    [Test]
    public async Task RunAsync_UnknownCommand_ReturnsNonZero()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.Unknown, 8080, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(error.ToString()).Contains("Unknown");
    }

    /// <summary>
    ///     Verifies that HarSummary without a path argument returns an error code.
    /// </summary>
    [Test]
    public async Task RunAsync_HarSummaryWithoutPath_ReturnsError()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.HarSummary, 8080, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(4);
        await Assert.That(error.ToString()).Contains("har-summary");
    }

    /// <summary>
    ///     Verifies that HarSummary with a nonexistent file returns a file-not-found error.
    /// </summary>
    [Test]
    public async Task RunAsync_HarSummaryMissingFile_ReturnsError()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.HarSummary, 8080, Path.Combine(Path.GetTempPath(), "definitelynotreal_" + System.Guid.NewGuid() + ".har"));

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(5);
        await Assert.That(error.ToString()).Contains("File not found");
    }

    /// <summary>
    ///     Verifies that HarSummary with a valid HAR file renders the summary and returns zero.
    /// </summary>
    [Test]
    public async Task RunAsync_HarSummaryValidFile_RendersSummary()
    {
        var temporaryFile = Path.Combine(Path.GetTempPath(), "proxyfan_cli_test_" + System.Guid.NewGuid() + ".har");
        const string harJson = "{\"log\":{\"version\":\"1.2\",\"creator\":{\"name\":\"Test\",\"version\":\"1\"},\"entries\":[]}}";
        await File.WriteAllTextAsync(temporaryFile, harJson, Encoding.UTF8, CancellationToken.None);

        try
        {
            var runner = new CliRunner();
            using var output = new StringWriter();
            using var error = new StringWriter();
            var command = new CliCommand(CliCommandKind.HarSummary, 8080, temporaryFile);

            var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(output.ToString()).Contains("0 flow(s)");
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    /// <summary>
    ///     Verifies that the HarStats command is dispatched through <see cref="CliRunner" />
    ///     and renders the handler output for a valid HAR file.
    /// </summary>
    [Test]
    public async Task RunAsync_HarStatsCommand_RoutesThroughRunner()
    {
        var temporaryFile = Path.Combine(Path.GetTempPath(), "proxyfan_cli_stats_" + System.Guid.NewGuid() + ".har");
        const string harJson = """
            {"log":{"version":"1.2","creator":{"name":"Test","version":"1"},"entries":[
                {"startedDateTime":"2025-01-01T00:00:00Z","time":120,"request":{"method":"GET","url":"https://api.example.com/v1/users","httpVersion":"HTTP/1.1","headers":[],"bodySize":0},
                 "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{"size":1024}}},
                {"startedDateTime":"2025-01-01T00:00:01Z","time":50,"request":{"method":"POST","url":"https://api.example.com/v1/login","httpVersion":"HTTP/1.1","headers":[],"postData":{"text":"a=1","mimeType":"text/plain"}},
                 "response":{"status":404,"statusText":"Not Found","httpVersion":"HTTP/1.1","headers":[],"content":{"size":256}}},
                {"startedDateTime":"2025-01-01T00:00:02Z","time":200,"request":{"method":"GET","url":"https://api.example.com/v1/products","httpVersion":"HTTP/1.1","headers":[]},
                 "response":{"status":500,"statusText":"Internal Server Error","httpVersion":"HTTP/1.1","headers":[],"content":{"size":512}}}
            ]}}
            """;
        await File.WriteAllTextAsync(temporaryFile, harJson, Encoding.UTF8, CancellationToken.None);

        try
        {
            var runner = new CliRunner();
            using var output = new StringWriter();
            using var error = new StringWriter();
            var command = CliArgumentParser.Parse(["har-stats", temporaryFile]);

            var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(output.ToString()).Contains("Total flows: 3");
            await Assert.That(output.ToString()).Contains("Methods:");
            await Assert.That(error.ToString()).IsEmpty();
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    /// <summary>
    ///     Verifies that the HarStats command emits the expected machine-readable JSON schema
    ///     when the <c>--json</c> flag is supplied.
    /// </summary>
    [Test]
    public async Task RunAsync_HarStatsCommand_JsonOutputMatchesSchema()
    {
        var temporaryFile = Path.Combine(Path.GetTempPath(), "proxyfan_cli_stats_json_" + System.Guid.NewGuid() + ".har");
        const string harJson = """
            {"log":{"version":"1.2","creator":{"name":"Test","version":"1"},"entries":[
                {"startedDateTime":"2025-01-01T00:00:00Z","time":120,"request":{"method":"GET","url":"https://api.example.com/v1/users","httpVersion":"HTTP/1.1","headers":[],"bodySize":0},
                 "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{"size":1024}}},
                {"startedDateTime":"2025-01-01T00:00:01Z","time":50,"request":{"method":"POST","url":"https://api.example.com/v1/login","httpVersion":"HTTP/1.1","headers":[],"postData":{"text":"a=1","mimeType":"text/plain"}},
                 "response":{"status":404,"statusText":"Not Found","httpVersion":"HTTP/1.1","headers":[],"content":{"size":256}}},
                {"startedDateTime":"2025-01-01T00:00:02Z","time":200,"request":{"method":"GET","url":"https://api.example.com/v1/products","httpVersion":"HTTP/1.1","headers":[]},
                 "response":{"status":500,"statusText":"Internal Server Error","httpVersion":"HTTP/1.1","headers":[],"content":{"size":512}}}
            ]}}
            """;
        await File.WriteAllTextAsync(temporaryFile, harJson, Encoding.UTF8, CancellationToken.None);

        try
        {
            var runner = new CliRunner();
            using var output = new StringWriter();
            using var error = new StringWriter();
            var command = CliArgumentParser.Parse(["har-stats", temporaryFile, "--json"]);

            var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(error.ToString()).IsEmpty();

            using var document = JsonDocument.Parse(output.ToString());
            var root = document.RootElement;
            await Assert.That(root.GetProperty("totalFlows").GetInt32()).IsEqualTo(3);
            await Assert.That(root.GetProperty("statusClasses").GetProperty("2xx").GetInt32()).IsEqualTo(1);
            await Assert.That(root.GetProperty("statusClasses").GetProperty("4xx").GetInt32()).IsEqualTo(1);
            await Assert.That(root.GetProperty("statusClasses").GetProperty("5xx").GetInt32()).IsEqualTo(1);
            await Assert.That(root.GetProperty("methods").GetProperty("GET").GetInt32()).IsEqualTo(2);
            await Assert.That(root.GetProperty("methods").GetProperty("POST").GetInt32()).IsEqualTo(1);
            await Assert.That(root.GetProperty("requestBodyBytes").GetInt64()).IsEqualTo(3);
            await Assert.That(root.GetProperty("responseBodyBytes").GetInt64()).IsEqualTo(0);
            var duration = root.GetProperty("durationMilliseconds");
            var min = duration.GetProperty("min").GetDouble();
            var median = duration.GetProperty("median").GetDouble();
            var max = duration.GetProperty("max").GetDouble();
            await Assert.That(duration.GetProperty("samples").GetInt32()).IsEqualTo(3);
            await Assert.That(min <= median).IsTrue();
            await Assert.That(median <= max).IsTrue();
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    /// <summary>
    ///     Verifies that the Send command routes through CliSendHandler.
    /// </summary>
    [Test]
    public async Task RunAsync_SendCommand_RoutesThroughHandler()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var request = new CliSendRequest("GET", "https://example.com/", new System.Collections.Generic.Dictionary<string, string>(), null);
        var command = new CliCommand(CliCommandKind.Send, 8080, null, request);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("GET / HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that an unrecognized CliCommandKind enum value hits the default branch
    ///     and returns exit code 3.
    /// </summary>
    [Test]
    public async Task RunAsync_UnhandledKind_ReturnsThree()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand((CliCommandKind)999, 8080, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(3);
        await Assert.That(error.ToString()).Contains("Unhandled");
    }

    /// <summary>
    ///     Verifies that the HarToCurl command is dispatched and reaches its handler when
    ///     the path argument is missing (which returns its handler-specific error code 7).
    /// </summary>
    [Test]
    public async Task RunAsync_HarToCurlWithoutPath_RoutesToHandler()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.HarToCurl, 8080, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(7);
        await Assert.That(error.ToString()).Contains("har-to-curl");
    }

    /// <summary>
    ///     Verifies that the HarToCurl command renders a cURL line per HAR entry when given
    ///     a valid file with at least one request.
    /// </summary>
    [Test]
    public async Task RunAsync_HarToCurlValidFile_WritesCurlLine()
    {
        var temporaryFile = Path.Combine(Path.GetTempPath(), "proxyfan_cli_test_" + System.Guid.NewGuid() + ".har");
        const string harJson = "{\"log\":{\"version\":\"1.2\",\"creator\":{\"name\":\"Test\",\"version\":\"1\"},\"entries\":[{\"startedDateTime\":\"2024-01-01T00:00:00Z\",\"time\":0,\"request\":{\"method\":\"GET\",\"url\":\"https://example.com/\",\"httpVersion\":\"HTTP/1.1\",\"headers\":[],\"queryString\":[],\"cookies\":[],\"headersSize\":-1,\"bodySize\":-1},\"response\":{\"status\":200,\"statusText\":\"OK\",\"httpVersion\":\"HTTP/1.1\",\"headers\":[],\"cookies\":[],\"content\":{\"size\":0,\"mimeType\":\"text/plain\"},\"redirectURL\":\"\",\"headersSize\":-1,\"bodySize\":0},\"cache\":{},\"timings\":{\"send\":0,\"wait\":0,\"receive\":0}}]}}";
        await File.WriteAllTextAsync(temporaryFile, harJson, Encoding.UTF8, CancellationToken.None);

        try
        {
            var runner = new CliRunner();
            using var output = new StringWriter();
            using var error = new StringWriter();
            var command = new CliCommand(CliCommandKind.HarToCurl, 8080, temporaryFile);

            var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(output.ToString()).Contains("curl");
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    /// <summary>
    ///     Verifies the HarFilter command is dispatched and reaches its handler when options
    ///     are missing (returns the handler's error code).
    /// </summary>
    [Test]
    public async Task RunAsync_HarFilterWithoutOptions_RoutesToHandler()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.HarFilter, 8080, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(9);
        await Assert.That(error.ToString()).Contains("har-filter requires");
    }

    /// <summary>
    ///     Verifies that <c>start --json</c> preserves a machine-readable automation contract
    ///     through the runner. On Windows it reports a listening event; on other platforms it
    ///     reports the existing unsupported-platform error as JSON.
    /// </summary>
    [Test]
    public async Task RunAsync_StartCommandJsonOutput_ExitCodeAndOutputMatchAutomationContract()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();

        if (OperatingSystem.IsWindows())
        {
            var command = CliArgumentParser.Parse(["start", "--port", "9999", "--duration", "1", "--json"]);

            var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(error.ToString()).IsEmpty();

            var lines = output.ToString()
                .Split(['\r', '\n'], System.StringSplitOptions.RemoveEmptyEntries);
            using var document = JsonDocument.Parse(lines[0]);
            var root = document.RootElement;
            await Assert.That(root.GetProperty("status").GetString()).IsEqualTo("listening");
            await Assert.That(root.GetProperty("port").GetInt32()).IsEqualTo(9999);
        }
        else
        {
            var command = CliArgumentParser.Parse(["start", "--port", "9999", "--json"]);

            var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(5);
            await Assert.That(output.ToString()).IsEmpty();

            using var document = JsonDocument.Parse(error.ToString());
            var root = document.RootElement;
            await Assert.That(root.GetProperty("exitCode").GetInt32()).IsEqualTo(5);
            await Assert.That(root.GetProperty("status").GetString()).IsEqualTo("error");
            await Assert.That(root.GetProperty("error").GetString()).IsEqualTo("The 'start' command requires Windows.");
        }
    }
}
