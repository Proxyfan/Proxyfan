using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliHarFilterHandler" /> and <see cref="CliHarFilterArgumentParser" />.
/// </summary>
public sealed class CliHarFilterHandlerTests
{
    private const string HarJson = """
        {"log":{"version":"1.2","creator":{"name":"T","version":"1"},"entries":[
            {"startedDateTime":"2025-01-01T00:00:00Z","request":{"method":"GET","url":"https://api.example.com/v1/users","httpVersion":"HTTP/1.1","headers":[]},
             "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{}}},
            {"startedDateTime":"2025-01-01T00:00:01Z","request":{"method":"GET","url":"https://cdn.example.com/assets/style.css","httpVersion":"HTTP/1.1","headers":[]},
             "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{}}},
            {"startedDateTime":"2025-01-01T00:00:02Z","request":{"method":"POST","url":"https://api.example.com/v1/login","httpVersion":"HTTP/1.1","headers":[]},
             "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{}}}
        ]}}
        """;

    /// <summary>
    ///     The parser returns null when any required flag is missing.
    /// </summary>
    [Test]
    public async Task Parse_MissingPattern_ReturnsNull()
    {
        var options = CliHarFilterArgumentParser.Parse(["har-filter", "--input", "a.har", "--output", "b.har"]);

        await Assert.That(options).IsNull();
    }

    /// <summary>
    ///     The parser returns null when --input is missing.
    /// </summary>
    [Test]
    public async Task Parse_MissingInput_ReturnsNull()
    {
        var options = CliHarFilterArgumentParser.Parse(["har-filter", "--output", "b.har", "--pattern", "*"]);

        await Assert.That(options).IsNull();
    }

    /// <summary>
    ///     The parser returns null when --output is missing.
    /// </summary>
    [Test]
    public async Task Parse_MissingOutput_ReturnsNull()
    {
        var options = CliHarFilterArgumentParser.Parse(["har-filter", "--input", "a.har", "--pattern", "*"]);

        await Assert.That(options).IsNull();
    }

    /// <summary>
    ///     The parser returns the supplied paths and pattern.
    /// </summary>
    [Test]
    public async Task Parse_AllFlagsPresent_ReturnsOptions()
    {
        var options = CliHarFilterArgumentParser.Parse(
            ["har-filter", "--input", "a.har", "--output", "b.har", "--pattern", "*.example.com/api/*"]);

        await Assert.That(options).IsNotNull();
        await Assert.That(options!.InputPath).IsEqualTo("a.har");
        await Assert.That(options.OutputPath).IsEqualTo("b.har");
        await Assert.That(options.Pattern).IsEqualTo("*.example.com/api/*");
    }

    /// <summary>
    ///     The handler returns error code 9 when options are missing.
    /// </summary>
    [Test]
    public async Task RunAsync_MissingOptions_ReturnsErrorNine()
    {
        var command = new CliCommand(CliCommandKind.HarFilter, 8080, null);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliHarFilterHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(9);
        await Assert.That(error.ToString()).Contains("har-filter requires");
    }

    /// <summary>
    ///     The handler returns error code 10 when the input file is missing.
    /// </summary>
    [Test]
    public async Task RunAsync_MissingInputFile_ReturnsErrorTen()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"proxyfan-missing-{Guid.NewGuid():N}.har");
        var command = new CliCommand(CliCommandKind.HarFilter, 8080, nonExistent)
        {
            HarFilterOptions = new CliHarFilterOptions
            {
                InputPath = nonExistent,
                OutputPath = Path.Combine(Path.GetTempPath(), "out.har"),
                Pattern = "*",
            },
        };
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliHarFilterHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(10);
    }

    /// <summary>
    ///     A wildcard pattern matches only the api.example.com flows; cdn.example.com is excluded.
    /// </summary>
    [Test]
    public async Task RunAsync_WildcardPattern_FiltersToMatchingFlows()
    {
        var inputPath = CreateTempPath("input");
        var outputPath = CreateTempPath("output");

        try
        {
            await File.WriteAllTextAsync(inputPath, HarJson, Encoding.UTF8, CancellationToken.None);
            var command = new CliCommand(CliCommandKind.HarFilter, 8080, inputPath)
            {
                HarFilterOptions = new CliHarFilterOptions
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    Pattern = "https://api.example.com/*",
                },
            };
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliHarFilterHandler.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(File.Exists(outputPath)).IsTrue();
            var filtered = await File.ReadAllTextAsync(outputPath, CancellationToken.None);
            await Assert.That(filtered).Contains("api.example.com/v1/users");
            await Assert.That(filtered).Contains("api.example.com/v1/login");
            await Assert.That(filtered).DoesNotContain("cdn.example.com");
            await Assert.That(output.ToString()).Contains("Filtered 2 flow(s) of 3");
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(outputPath);
        }
    }

    /// <summary>
    ///     A pattern that matches nothing yields an empty HAR output.
    /// </summary>
    [Test]
    public async Task RunAsync_NoMatches_WritesEmptyHar()
    {
        var inputPath = CreateTempPath("input");
        var outputPath = CreateTempPath("output");

        try
        {
            await File.WriteAllTextAsync(inputPath, HarJson, Encoding.UTF8, CancellationToken.None);
            var command = new CliCommand(CliCommandKind.HarFilter, 8080, inputPath)
            {
                HarFilterOptions = new CliHarFilterOptions
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    Pattern = "https://no-such-host.invalid/*",
                },
            };
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliHarFilterHandler.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(File.Exists(outputPath)).IsTrue();
            await Assert.That(output.ToString()).Contains("Filtered 0 flow(s) of 3");
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(outputPath);
        }
    }

    /// <summary>
    ///     The handler creates the output directory if it does not exist.
    /// </summary>
    [Test]
    public async Task RunAsync_OutputDirectoryMissing_CreatesDirectory()
    {
        var inputPath = CreateTempPath("input");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"proxyfan-cli-out-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(outputDirectory, "filtered.har");

        try
        {
            await File.WriteAllTextAsync(inputPath, HarJson, Encoding.UTF8, CancellationToken.None);
            var command = new CliCommand(CliCommandKind.HarFilter, 8080, inputPath)
            {
                HarFilterOptions = new CliHarFilterOptions
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    Pattern = "*",
                },
            };
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliHarFilterHandler.RunAsync(command, output, error, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(Directory.Exists(outputDirectory)).IsTrue();
            await Assert.That(File.Exists(outputPath)).IsTrue();
        }
        finally
        {
            DeleteIfExists(inputPath);

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static string CreateTempPath(string tag)
    {
        return Path.Combine(Path.GetTempPath(), $"proxyfan-cli-{tag}-{Guid.NewGuid():N}.har");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
