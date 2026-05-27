using Proxyfan.Domain.Rules.Rules;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="MapRemoteUriRewriter" />.
/// </summary>
public sealed class MapRemoteUriRewriterTests
{
    /// <summary>
    ///     When all destination components are non-null, all are applied to the URI.
    /// </summary>
    [Test]
    public async Task Rewrite_AllComponents_AppliesAll()
    {
        var original = new Uri("https://public.example.com/old/path?query=value");
        var destination = new MapRemoteDestination(scheme: "http", host: "internal", port: 9090, path: "/new/api", isPreservingHostHeader: false);

        var rewritten = MapRemoteUriRewriter.Rewrite(original, destination);

        await Assert.That(rewritten.Scheme).IsEqualTo("http");
        await Assert.That(rewritten.Host).IsEqualTo("internal");
        await Assert.That(rewritten.Port).IsEqualTo(9090);
        await Assert.That(rewritten.AbsolutePath).IsEqualTo("/new/api");
    }

    /// <summary>
    ///     Null components preserve the corresponding original component.
    /// </summary>
    [Test]
    public async Task Rewrite_NullComponents_PreservesOriginalComponents()
    {
        var original = new Uri("https://public.example.com:8443/api/users");
        var destination = new MapRemoteDestination(scheme: null, host: null, port: null, path: null, isPreservingHostHeader: false);

        var rewritten = MapRemoteUriRewriter.Rewrite(original, destination);

        await Assert.That(rewritten.Scheme).IsEqualTo("https");
        await Assert.That(rewritten.Host).IsEqualTo("public.example.com");
        await Assert.That(rewritten.Port).IsEqualTo(8443);
        await Assert.That(rewritten.AbsolutePath).IsEqualTo("/api/users");
    }

    /// <summary>
    ///     Replacing only the host preserves scheme, port, and path.
    /// </summary>
    [Test]
    public async Task Rewrite_OnlyHost_ReplacesHostOnly()
    {
        var original = new Uri("https://public.example.com:8443/path");
        var destination = new MapRemoteDestination(scheme: null, host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);

        var rewritten = MapRemoteUriRewriter.Rewrite(original, destination);

        await Assert.That(rewritten.Host).IsEqualTo("internal.example.com");
        await Assert.That(rewritten.Scheme).IsEqualTo("https");
        await Assert.That(rewritten.Port).IsEqualTo(8443);
        await Assert.That(rewritten.AbsolutePath).IsEqualTo("/path");
    }
}
