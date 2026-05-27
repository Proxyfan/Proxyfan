using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliHarToCurlHandler" />.
/// </summary>
public sealed class CliHarToCurlHandlerTests
{
    /// <summary>
    ///     Verifies that a valid HAR with one entry produces one cURL command line.
    /// </summary>
    [Test]
    public async Task RunAsync_ValidHarOneEntry_PrintsOneCurlCommand()
    {
        var harJson = """
            {"log":{"version":"1.2","creator":{"name":"T","version":"1"},"entries":[
                {"startedDateTime":"2025-01-01T00:00:00Z","request":{"method":"GET","url":"https://example.com/","httpVersion":"HTTP/1.1","headers":[{"name":"Accept","value":"application/json"}]},
                 "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{}}}
            ]}}
            """;
        var tempFile = Path.Combine(Path.GetTempPath(), "proxyfan_har_curl_" + Guid.NewGuid() + ".har");
        await File.WriteAllTextAsync(tempFile, harJson, Encoding.UTF8, CancellationToken.None);

        try
        {
            var command = new CliCommand(CliCommandKind.HarToCurl, 8080, tempFile);
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliHarToCurlHandler.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(output.ToString()).StartsWith("curl -X GET");
            await Assert.That(output.ToString()).Contains("Accept: application/json");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    ///     Verifies that an empty path returns error code 7.
    /// </summary>
    [Test]
    public async Task RunAsync_EmptyPath_ReturnsErrorSeven()
    {
        var command = new CliCommand(CliCommandKind.HarToCurl, 8080, null);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliHarToCurlHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(7);
    }

    /// <summary>
    ///     Verifies that a missing file returns error code 8.
    /// </summary>
    [Test]
    public async Task RunAsync_MissingFile_ReturnsErrorEight()
    {
        var command = new CliCommand(CliCommandKind.HarToCurl, 8080, Path.Combine(Path.GetTempPath(), "nope_" + Guid.NewGuid() + ".har"));
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliHarToCurlHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(8);
    }

    /// <summary>
    ///     Verifies that an entry with no request is skipped silently.
    /// </summary>
    [Test]
    public async Task RunAsync_EntryWithoutRequest_SkipsSilently()
    {
        var harJson = """
            {"log":{"entries":[{"startedDateTime":"2025-01-01T00:00:00Z"}]}}
            """;
        var tempFile = Path.Combine(Path.GetTempPath(), "proxyfan_har_curl_skip_" + Guid.NewGuid() + ".har");
        await File.WriteAllTextAsync(tempFile, harJson, Encoding.UTF8, CancellationToken.None);

        try
        {
            var command = new CliCommand(CliCommandKind.HarToCurl, 8080, tempFile);
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliHarToCurlHandler.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(output.ToString()).IsEqualTo(string.Empty);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
