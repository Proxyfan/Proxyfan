using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="Domain.Rules.Rules.MapRemoteDestination" />.
/// </summary>
public sealed class MapRemoteDestinationTests
{
    /// <summary>
    ///     Verifies that the constructor stores supplied values.
    /// </summary>
    [Test]
    public async Task Constructor_WithAllValues_StoresValues()
    {
        var destination = new Domain.Rules.Rules.MapRemoteDestination(
            scheme: "http",
            host: "localhost",
            port: 8080,
            path: "/api",
            isPreservingHostHeader: true);

        await Assert.That(destination.Scheme).IsEqualTo("http");
        await Assert.That(destination.Host).IsEqualTo("localhost");
        await Assert.That(destination.Port).IsEqualTo(8080);
        await Assert.That(destination.Path).IsEqualTo("/api");
        await Assert.That(destination.IsPreservingHostHeader).IsTrue();
    }

    /// <summary>
    ///     Verifies that an empty host (zero-length string) is rejected.
    /// </summary>
    [Test]
    public async Task Constructor_WithEmptyHost_Throws()
    {
        await Assert.That(() => _ = new Domain.Rules.Rules.MapRemoteDestination(
            scheme: null,
            host: string.Empty,
            port: null,
            path: null,
            isPreservingHostHeader: false)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that a port below 1 is rejected.
    /// </summary>
    [Test]
    public async Task Constructor_WithPortBelowRange_Throws()
    {
        await Assert.That(() => _ = new Domain.Rules.Rules.MapRemoteDestination(
            scheme: null,
            host: null,
            port: 0,
            path: null,
            isPreservingHostHeader: false)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that a port above 65535 is rejected.
    /// </summary>
    [Test]
    public async Task Constructor_WithPortAboveRange_Throws()
    {
        await Assert.That(() => _ = new Domain.Rules.Rules.MapRemoteDestination(
            scheme: null,
            host: null,
            port: 65536,
            path: null,
            isPreservingHostHeader: false)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that null-only configuration is allowed (no-op rewrite).
    /// </summary>
    [Test]
    public async Task Constructor_WithAllNulls_Succeeds()
    {
        var destination = new Domain.Rules.Rules.MapRemoteDestination(
            scheme: null,
            host: null,
            port: null,
            path: null,
            isPreservingHostHeader: false);

        await Assert.That(destination.Scheme).IsNull();
        await Assert.That(destination.Host).IsNull();
        await Assert.That(destination.Port).IsNull();
        await Assert.That(destination.Path).IsNull();
    }
}
