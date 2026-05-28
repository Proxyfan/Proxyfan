using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    ///     Verifies that the Start command writes the port and returns zero.
    /// </summary>
    [Test]
    public async Task RunAsync_StartCommand_WritesPort()
    {
        var runner = new CliRunner();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var command = new CliCommand(CliCommandKind.Start, 9999, null);

        var exitCode = await runner.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("9999");
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
}
