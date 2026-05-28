using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliHarSummaryHandler" />.
/// </summary>
public sealed class CliHarSummaryHandlerTests
{
    [Test]
    public async Task RunAsync_WithoutPathArgument_ReturnsExitCodeFourWithError()
    {
        var command = new CliCommand(CliCommandKind.HarSummary, port: 0, pathArgument: null);
        using var standardOut = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliHarSummaryHandler.RunAsync(command, standardOut, standardError, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(4);
        await Assert.That(standardError.ToString()).Contains("requires a file path");
    }

    [Test]
    public async Task RunAsync_FileMissing_ReturnsExitCodeFiveWithError()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"proxyfan-missing-{Guid.NewGuid():N}.har");
        var command = new CliCommand(CliCommandKind.HarSummary, port: 0, pathArgument: missingPath);
        using var standardOut = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliHarSummaryHandler.RunAsync(command, standardOut, standardError, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(5);
        await Assert.That(standardError.ToString()).Contains("File not found");
    }

    [Test]
    public async Task RunAsync_ValidEmptyHarFile_ReturnsZeroAndWritesSummary()
    {
        const string harJson = "{\"log\":{\"version\":\"1.2\",\"creator\":{\"name\":\"Test\",\"version\":\"1\"},\"entries\":[]}}";
        var tempPath = Path.Combine(Path.GetTempPath(), $"proxyfan-har-{Guid.NewGuid():N}.har");
        await File.WriteAllTextAsync(tempPath, harJson, Encoding.UTF8);

        try
        {
            var command = new CliCommand(CliCommandKind.HarSummary, port: 0, pathArgument: tempPath);
            using var standardOut = new StringWriter();
            using var standardError = new StringWriter();

            var exitCode = await CliHarSummaryHandler.RunAsync(command, standardOut, standardError, CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(standardError.ToString()).IsEqualTo(string.Empty);
            await Assert.That(standardOut.ToString().Length).IsGreaterThan(0);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Test]
    public async Task RunAsync_EmptyStringPath_ReturnsExitCodeFour()
    {
        var command = new CliCommand(CliCommandKind.HarSummary, port: 0, pathArgument: "   ");
        using var standardOut = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliHarSummaryHandler.RunAsync(command, standardOut, standardError, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(4);
    }
}
