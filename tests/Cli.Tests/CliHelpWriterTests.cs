using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliHelpWriter" />.
/// </summary>
public sealed class CliHelpWriterTests
{
    /// <summary>
    ///     Verifies that the help text contains the product name, all commands, and option names.
    /// </summary>
    [Test]
    public async Task WriteHelpAsync_WhenInvoked_WritesAllCommandsAndOptions()
    {
        using var writer = new StringWriter();

        await CliHelpWriter.WriteHelpAsync(writer, CancellationToken.None);
        var text = writer.ToString();

        await Assert.That(text).Contains("Proxyfan");
        await Assert.That(text).Contains("help");
        await Assert.That(text).Contains("version");
        await Assert.That(text).Contains("start");
        await Assert.That(text).Contains("har-summary");
        await Assert.That(text).Contains("--port");
        await Assert.That(text).Contains("--bind-address");
        await Assert.That(text).Contains("--input");
    }
}
