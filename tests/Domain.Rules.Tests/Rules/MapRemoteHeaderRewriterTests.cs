using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="MapRemoteHeaderRewriter" />.
/// </summary>
public sealed class MapRemoteHeaderRewriterTests
{
    /// <summary>
    ///     Verifies that an existing Host header is replaced with the rewritten URI's host:port form.
    /// </summary>
    [Test]
    public async Task ReplaceHostHeader_ExistingHostHeader_IsReplacedWithRewrittenHost()
    {
        var headers = HeaderCollection.Empty.Add("Host", "old.example");
        var rewrittenUri = new Uri("https://new.example:8443/path");

        var result = MapRemoteHeaderRewriter.ReplaceHostHeader(headers, rewrittenUri);

        await Assert.That(result.Get("Host")).IsEqualTo("new.example:8443");
    }

    /// <summary>
    ///     Verifies that when the rewritten URI uses the default port, the Host value omits the port.
    /// </summary>
    [Test]
    public async Task ReplaceHostHeader_DefaultPort_OmitsPort()
    {
        var headers = HeaderCollection.Empty.Add("Host", "old.example");
        var rewrittenUri = new Uri("https://new.example/path");

        var result = MapRemoteHeaderRewriter.ReplaceHostHeader(headers, rewrittenUri);

        await Assert.That(result.Get("Host")).IsEqualTo("new.example");
    }

    /// <summary>
    ///     Verifies that when no Host header exists, a Host header is added.
    /// </summary>
    [Test]
    public async Task ReplaceHostHeader_NoHostHeader_AddsHostHeader()
    {
        var headers = HeaderCollection.Empty.Add("Accept", "*/*");
        var rewrittenUri = new Uri("https://new.example/");

        var result = MapRemoteHeaderRewriter.ReplaceHostHeader(headers, rewrittenUri);

        await Assert.That(result.Get("Host")).IsEqualTo("new.example");
        await Assert.That(result.Get("Accept")).IsEqualTo("*/*");
    }

    /// <summary>
    ///     Verifies that other headers are preserved verbatim.
    /// </summary>
    [Test]
    public async Task ReplaceHostHeader_OtherHeaders_ArePreserved()
    {
        var headers = HeaderCollection.Empty
            .Add("Accept", "application/json")
            .Add("Accept", "text/html")
            .Add("User-Agent", "Proxyfan/1.0")
            .Add("Host", "old.example");
        var rewrittenUri = new Uri("https://new.example/");

        var result = MapRemoteHeaderRewriter.ReplaceHostHeader(headers, rewrittenUri);

        await Assert.That(result.GetAll("Accept").Length).IsEqualTo(2);
        await Assert.That(result.Get("User-Agent")).IsEqualTo("Proxyfan/1.0");
        await Assert.That(result.Get("Host")).IsEqualTo("new.example");
    }

    /// <summary>
    ///     Verifies that case-insensitive Host header recognition works.
    /// </summary>
    [Test]
    public async Task ReplaceHostHeader_LowercaseHost_IsStillRecognized()
    {
        var headers = HeaderCollection.Empty.Add("host", "old.example");
        var rewrittenUri = new Uri("https://new.example/");

        var result = MapRemoteHeaderRewriter.ReplaceHostHeader(headers, rewrittenUri);

        // Only one Host header in the result (with new value)
        await Assert.That(result.GetAll("Host").Length).IsEqualTo(1);
        await Assert.That(result.Get("Host")).IsEqualTo("new.example");
    }
}
