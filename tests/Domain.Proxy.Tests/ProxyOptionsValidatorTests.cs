using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>Tests for <see cref="ProxyOptionsValidator" />.</summary>
internal sealed class ProxyOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(ProxyOptions options)
    {
        return new ProxyOptionsValidator().Validate(null, options);
    }

    /// <summary>Verifies that port 8080 (default) passes validation.</summary>
    [Test]
    public async Task Validate_Port8080_ReturnsSuccess()
    {
        var result = Validate(new ProxyOptions { Port = 8080 });
        await Assert.That(result.Succeeded).IsTrue();
    }

    /// <summary>Verifies that port 1024 (minimum) passes validation.</summary>
    [Test]
    public async Task Validate_Port1024_ReturnsSuccess()
    {
        var result = Validate(new ProxyOptions { Port = 1024 });
        await Assert.That(result.Succeeded).IsTrue();
    }

    /// <summary>Verifies that port 65535 (maximum) passes validation.</summary>
    [Test]
    public async Task Validate_Port65535_ReturnsSuccess()
    {
        var result = Validate(new ProxyOptions { Port = 65535 });
        await Assert.That(result.Succeeded).IsTrue();
    }

    /// <summary>Verifies that port 1023 (below minimum) fails validation.</summary>
    [Test]
    public async Task Validate_Port1023_ReturnsFail()
    {
        var result = Validate(new ProxyOptions { Port = 1023 });
        await Assert.That(result.Failed).IsTrue();
    }

    /// <summary>Verifies that port 0 fails validation.</summary>
    [Test]
    public async Task Validate_Port0_ReturnsFail()
    {
        var result = Validate(new ProxyOptions { Port = 0 });
        await Assert.That(result.Failed).IsTrue();
    }

    /// <summary>Verifies that port 65536 (above maximum) fails validation.</summary>
    [Test]
    public async Task Validate_Port65536_ReturnsFail()
    {
        var result = Validate(new ProxyOptions { Port = 65536 });
        await Assert.That(result.Failed).IsTrue();
    }

    /// <summary>Verifies that MaxConnections of 1 passes validation.</summary>
    [Test]
    public async Task Validate_MaxConnections1_ReturnsSuccess()
    {
        var result = Validate(new ProxyOptions { MaxConnections = 1 });
        await Assert.That(result.Succeeded).IsTrue();
    }

    /// <summary>Verifies that MaxConnections of 0 fails validation.</summary>
    [Test]
    public async Task Validate_MaxConnections0_ReturnsFail()
    {
        var result = Validate(new ProxyOptions { MaxConnections = 0 });
        await Assert.That(result.Failed).IsTrue();
    }

    /// <summary>Verifies that MaxConnections of -1 fails validation.</summary>
    [Test]
    public async Task Validate_MaxConnectionsNegative1_ReturnsFail()
    {
        var result = Validate(new ProxyOptions { MaxConnections = -1 });
        await Assert.That(result.Failed).IsTrue();
    }
}
