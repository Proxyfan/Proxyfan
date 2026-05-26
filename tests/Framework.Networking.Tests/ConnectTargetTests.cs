using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ConnectTarget" />.
/// </summary>
public sealed class ConnectTargetTests
{
    /// <summary>
    ///     Verifies that the constructor stores the supplied host and port.
    /// </summary>
    [Test]
    public async Task Constructor_WithValidArguments_StoresValues()
    {
        var target = new ConnectTarget("example.com", 443);

        await Assert.That(target.Host).IsEqualTo("example.com");
        await Assert.That(target.Port).IsEqualTo(443);
    }

    /// <summary>
    ///     Verifies that a null host throws <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithNullHost_ThrowsArgumentException()
    {
        await Assert.That(() => _ = new ConnectTarget(null!, 443)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that an empty host throws <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithEmptyHost_ThrowsArgumentException()
    {
        await Assert.That(() => _ = new ConnectTarget(string.Empty, 443)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that a whitespace host throws <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithWhitespaceHost_ThrowsArgumentException()
    {
        await Assert.That(() => _ = new ConnectTarget("   ", 443)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that a port below 1 throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithPortBelowRange_ThrowsArgumentOutOfRangeException()
    {
        await Assert.That(() => _ = new ConnectTarget("example.com", 0)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that a port above 65535 throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithPortAboveRange_ThrowsArgumentOutOfRangeException()
    {
        await Assert.That(() => _ = new ConnectTarget("example.com", 65536)).Throws<ArgumentOutOfRangeException>();
    }
}