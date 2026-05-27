using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ConnectTargetValidator" />.
/// </summary>
public sealed class ConnectTargetValidatorTests
{
    /// <summary>
    ///     Verifies a well-formed target is accepted.
    /// </summary>
    [Test]
    [Arguments("example.com", 80)]
    [Arguments("example.com", 443)]
    [Arguments("192.168.1.1", 8080)]
    [Arguments("api.svc.internal", 1)]
    [Arguments("api.svc.internal", 65535)]
    public async Task HasValidTarget_WellFormed_ReturnsTrue(string host, int port)
    {
        var result = ConnectTargetValidator.HasValidTarget(host, port);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies null/empty host is rejected.
    /// </summary>
    [Test]
    [Arguments(null, 443)]
    [Arguments("", 443)]
    [Arguments("   ", 443)]
    public async Task HasValidTarget_BlankHost_ReturnsFalse(string? host, int port)
    {
        var result = ConnectTargetValidator.HasValidTarget(host, port);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies out-of-range ports are rejected.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(65536)]
    [Arguments(100000)]
    public async Task HasValidTarget_OutOfRangePort_ReturnsFalse(int port)
    {
        var result = ConnectTargetValidator.HasValidTarget("example.com", port);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies hosts containing CR or LF are rejected (header injection guard).
    /// </summary>
    [Test]
    [Arguments("example.com\r\nInjected: header", 443)]
    [Arguments("example.com\nfoo", 443)]
    [Arguments("example.com\r", 443)]
    public async Task HasValidTarget_HostWithNewline_ReturnsFalse(string host, int port)
    {
        var result = ConnectTargetValidator.HasValidTarget(host, port);

        await Assert.That(result).IsFalse();
    }
}
