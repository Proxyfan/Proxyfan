using System;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliArgumentParser" />.
/// </summary>
public sealed class CliArgumentParserTests
{
    /// <summary>
    ///     Verifies that no arguments returns the Help command.
    /// </summary>
    [Test]
    public async Task Parse_NoArguments_ReturnsHelp()
    {
        var command = CliArgumentParser.Parse(Array.Empty<string>());

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Help);
    }

    /// <summary>
    ///     Verifies that "--help" and "help" both produce the Help command.
    /// </summary>
    /// <param name="token">The help-style token to parse.</param>
    [Test]
    [Arguments("--help")]
    [Arguments("-h")]
    [Arguments("help")]
    [Arguments("HELP")]
    public async Task Parse_HelpToken_ReturnsHelp(string token)
    {
        var args = ParserTestArguments.One(token);

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Help);
    }

    /// <summary>
    ///     Verifies that "--version" and "version" both produce the Version command.
    /// </summary>
    /// <param name="token">The version-style token to parse.</param>
    [Test]
    [Arguments("--version")]
    [Arguments("-v")]
    [Arguments("version")]
    public async Task Parse_VersionToken_ReturnsVersion(string token)
    {
        var args = ParserTestArguments.One(token);

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Version);
    }

    /// <summary>
    ///     Verifies that "start" without --port defaults to port 8080.
    /// </summary>
    [Test]
    public async Task Parse_StartWithoutPort_DefaultsToPort8080()
    {
        var args = ParserTestArguments.One("start");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Start);
        await Assert.That(command.Port).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that "start --port 9000" returns port 9000.
    /// </summary>
    [Test]
    public async Task Parse_StartWithPort_UsesGivenPort()
    {
        var args = ParserTestArguments.Three("start", "--port", "9000");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Start);
        await Assert.That(command.Port).IsEqualTo(9000);
    }

    /// <summary>
    ///     Verifies that "start --port BAD" falls back to default 8080.
    /// </summary>
    [Test]
    public async Task Parse_StartWithInvalidPort_FallsBackToDefault()
    {
        var args = ParserTestArguments.Three("start", "--port", "notanumber");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Port).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that "har-summary path/to/file.har" returns HarSummary with the path.
    /// </summary>
    [Test]
    public async Task Parse_HarSummaryWithPath_ReturnsHarSummary()
    {
        var args = ParserTestArguments.Two("har-summary", "capture.har");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.HarSummary);
        await Assert.That(command.PathArgument).IsEqualTo("capture.har");
    }

    /// <summary>
    ///     Verifies that "har-summary --input path/to/file.har" uses the --input flag.
    /// </summary>
    [Test]
    public async Task Parse_HarSummaryWithInputFlag_UsesInputPath()
    {
        var args = ParserTestArguments.Three("har-summary", "--input", "capture.har");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.PathArgument).IsEqualTo("capture.har");
    }

    /// <summary>
    ///     Verifies that an unknown command returns Unknown.
    /// </summary>
    [Test]
    public async Task Parse_UnknownCommand_ReturnsUnknown()
    {
        var args = ParserTestArguments.One("blahblah");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Unknown);
    }

    /// <summary>
    ///     Verifies that a port above 65535 falls back to default.
    /// </summary>
    [Test]
    public async Task Parse_StartWithOutOfRangePort_FallsBackToDefault()
    {
        var args = ParserTestArguments.Three("start", "--port", "99999");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Port).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that "har-summary" with NO args returns HarSummary with null path.
    /// </summary>
    [Test]
    public async Task Parse_HarSummaryWithNoPath_ReturnsHarSummaryWithNullPath()
    {
        var args = ParserTestArguments.One("har-summary");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.HarSummary);
        await Assert.That(command.PathArgument).IsNull();
    }

    /// <summary>
    ///     Verifies that "send --url ..." returns the Send command.
    /// </summary>
    [Test]
    public async Task Parse_SendWithUrl_ReturnsSend()
    {
        var args = ParserTestArguments.Three("send", "--url", "https://example.com/");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Send);
        await Assert.That(command.SendRequest).IsNotNull();
    }

    /// <summary>
    ///     Verifies that "har-to-curl path/to/file.har" returns HarToCurl with the path.
    /// </summary>
    [Test]
    public async Task Parse_HarToCurlWithPath_ReturnsHarToCurl()
    {
        var args = ParserTestArguments.Two("har-to-curl", "capture.har");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.HarToCurl);
        await Assert.That(command.PathArgument).IsEqualTo("capture.har");
    }

    /// <summary>
    ///     Verifies that "har-to-curl --input path" uses the --input flag.
    /// </summary>
    [Test]
    public async Task Parse_HarToCurlWithInputFlag_UsesInputPath()
    {
        var args = ParserTestArguments.Three("har-to-curl", "--input", "capture.har");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.HarToCurl);
        await Assert.That(command.PathArgument).IsEqualTo("capture.har");
    }

    /// <summary>
    ///     Verifies that --input later in the arg list is still found (covers the false-branch
    ///     of the per-iteration "is current arg --input" check in ExtractPath).
    /// </summary>
    [Test]
    public async Task Parse_HarSummaryWithInputFlagAfterOtherArgs_FindsInputPath()
    {
        var args = ParserTestArguments.Four("har-summary", "padding", "--input", "capture.har");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.HarSummary);
        await Assert.That(command.PathArgument).IsEqualTo("capture.har");
    }

    /// <summary>
    ///     Verifies that --port later in the arg list is still found (covers the false-branch
    ///     of the per-iteration "is current arg --port" check in ExtractPort).
    /// </summary>
    [Test]
    public async Task Parse_StartWithPortFlagAfterOtherArgs_FindsPort()
    {
        var args = ParserTestArguments.Four("start", "padding", "--port", "9000");

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Start);
        await Assert.That(command.Port).IsEqualTo(9000);
    }

    /// <summary>
    ///     Verifies that the har-filter verb with all required flags returns HarFilter
    ///     with the populated options.
    /// </summary>
    [Test]
    public async Task Parse_HarFilterWithAllFlags_ReturnsHarFilterWithOptions()
    {
        string[] args = ["har-filter", "--input", "a.har", "--output", "b.har", "--pattern", "*.example.com/*"];

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.HarFilter);
        await Assert.That(command.HarFilterOptions).IsNotNull();
        await Assert.That(command.HarFilterOptions!.InputPath).IsEqualTo("a.har");
        await Assert.That(command.HarFilterOptions.OutputPath).IsEqualTo("b.har");
        await Assert.That(command.HarFilterOptions.Pattern).IsEqualTo("*.example.com/*");
    }

    /// <summary>
    ///     Verifies that the har-filter verb with no flags returns HarFilter with null options
    ///     so the handler can surface a helpful error message.
    /// </summary>
    [Test]
    public async Task Parse_HarFilterWithoutFlags_ReturnsHarFilterWithNullOptions()
    {
        string[] args = ["har-filter"];

        var command = CliArgumentParser.Parse(args);

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.HarFilter);
        await Assert.That(command.HarFilterOptions).IsNull();
    }
}
