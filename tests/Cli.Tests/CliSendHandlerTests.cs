using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliSendHandler" />.
/// </summary>
public sealed class CliSendHandlerTests
{
    /// <summary>
    ///     Verifies that a valid Send command writes the formatted request and returns 0.
    /// </summary>
    [Test]
    public async Task RunAsync_ValidSendRequest_WritesRequestAndReturnsZero()
    {
        var request = new CliSendRequest("GET", "https://example.com/", new Dictionary<string, string>(), null);
        var command = new CliCommand(CliCommandKind.Send, 8080, null, request);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliSendHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("GET / HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that a Send command without a SendRequest writes an error and returns 6.
    /// </summary>
    [Test]
    public async Task RunAsync_MissingSendRequest_ReturnsError()
    {
        var command = new CliCommand(CliCommandKind.Send, 8080, null);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliSendHandler.RunAsync(command, output, error, CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(6);
        await Assert.That(error.ToString()).Contains("send");
    }
}
