using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliSendRequest" />.
/// </summary>
public sealed class CliSendRequestTests
{
    [Test]
    public async Task Constructor_WithBody_StoresAllProperties()
    {
        var headers = new Dictionary<string, string> { ["X-Test"] = "1" };

        var request = new CliSendRequest("POST", "https://example.com/api", headers, "payload");

        await Assert.That(request.Method).IsEqualTo("POST");
        await Assert.That(request.Url).IsEqualTo("https://example.com/api");
        await Assert.That(request.Headers).IsSameReferenceAs(headers);
        await Assert.That(request.Body).IsEqualTo("payload");
    }

    [Test]
    public async Task Constructor_WithoutBody_StoresNullBody()
    {
        var request = new CliSendRequest("GET", "https://example.com/", new Dictionary<string, string>(), body: null);

        await Assert.That(request.Body).IsNull();
    }
}
