using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliHarStatsHandler" /> and <see cref="HarStatsFormatter" />.
/// </summary>
public sealed class CliHarStatsHandlerTests
{
    private const string HarJson = """
        {"log":{"version":"1.2","creator":{"name":"T","version":"1"},"entries":[
            {"startedDateTime":"2025-01-01T00:00:00Z","time":120,"request":{"method":"GET","url":"https://api.example.com/v1/users","httpVersion":"HTTP/1.1","headers":[],"bodySize":0},
             "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{"size":1024}}},
            {"startedDateTime":"2025-01-01T00:00:01Z","time":50,"request":{"method":"POST","url":"https://api.example.com/v1/login","httpVersion":"HTTP/1.1","headers":[],"postData":{"text":"a=1","mimeType":"text/plain"}},
             "response":{"status":404,"statusText":"Not Found","httpVersion":"HTTP/1.1","headers":[],"content":{"size":256}}},
            {"startedDateTime":"2025-01-01T00:00:02Z","time":200,"request":{"method":"GET","url":"https://api.example.com/v1/products","httpVersion":"HTTP/1.1","headers":[]},
             "response":{"status":500,"statusText":"Internal Server Error","httpVersion":"HTTP/1.1","headers":[],"content":{"size":512}}}
        ]}}
        """;

    /// <summary>
    ///     Verifies missing path returns error code 11.
    /// </summary>
    [Test]
    public async Task RunAsync_MissingPath_ReturnsErrorEleven()
    {
        var command = new CliCommand(CliCommandKind.HarStats, 8080, null);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliHarStatsHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(11);
        await Assert.That(error.ToString()).Contains("har-stats requires");
    }

    /// <summary>
    ///     Verifies a missing file returns error code 12.
    /// </summary>
    [Test]
    public async Task RunAsync_MissingFile_ReturnsErrorTwelve()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"proxyfan-missing-{Guid.NewGuid():N}.har");
        var command = new CliCommand(CliCommandKind.HarStats, 8080, nonExistent);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliHarStatsHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(12);
    }

    /// <summary>
    ///     Verifies a valid HAR yields the totals, status distribution, and method distribution.
    /// </summary>
    [Test]
    public async Task RunAsync_ValidHar_PrintsTotalsAndDistributions()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"proxyfan-stats-{Guid.NewGuid():N}.har");

        try
        {
            await File.WriteAllTextAsync(tempPath, HarJson, Encoding.UTF8, CancellationToken.None);
            var command = new CliCommand(CliCommandKind.HarStats, 8080, tempPath);
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliHarStatsHandler.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            var text = output.ToString();
            await Assert.That(text).Contains("Total flows: 3");
            await Assert.That(text).Contains("Methods:");
            await Assert.That(text).Contains("GET");
            await Assert.That(text).Contains("POST");
            await Assert.That(text).Contains("Status classes:");
            await Assert.That(text).Contains("2xx");
            await Assert.That(text).Contains("4xx");
            await Assert.That(text).Contains("5xx");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    ///     Verifies the formatter returns just the total-flows line for an empty input.
    /// </summary>
    [Test]
    public async Task BuildReport_EmptyFlows_ReturnsOnlyTotal()
    {
        var report = HarStatsFormatter.BuildReport([]);

        await Assert.That(report.TrimEnd()).IsEqualTo("Total flows: 0");
    }
}
