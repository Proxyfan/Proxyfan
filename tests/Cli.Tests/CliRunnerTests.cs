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
}
