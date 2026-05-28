using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Cli;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliCommand" />.
/// </summary>
public sealed class CliCommandTests
{
    [Test]
    public async Task Constructor_ThreeArguments_DefaultsSendRequestToNull()
    {
        var command = new CliCommand(CliCommandKind.Start, port: 8080, pathArgument: "/tmp/x");

        await Assert.That(command.Kind).IsEqualTo(CliCommandKind.Start);
        await Assert.That(command.Port).IsEqualTo(8080);
        await Assert.That(command.PathArgument).IsEqualTo("/tmp/x");
        await Assert.That(command.SendRequest).IsNull();
    }

    [Test]
    public async Task Constructor_FourArguments_StoresSendRequest()
    {
        var send = new CliSendRequest("GET", "https://example.com", new Dictionary<string, string>(), null);

        var command = new CliCommand(CliCommandKind.Send, port: 0, pathArgument: null, sendRequest: send);

        await Assert.That(command.SendRequest).IsSameReferenceAs(send);
    }
}
