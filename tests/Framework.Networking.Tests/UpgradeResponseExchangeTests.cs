using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Unit tests for <see cref="UpgradeResponseExchange" />. Verifies that the record carries
///     both the parsed upstream response and the prefetched-byte payload through to the tunnel.
/// </summary>
public sealed class UpgradeResponseExchangeTests
{
    /// <summary>Constructor stores both the response exchange and the prefetched bytes.</summary>
    [Test]
    public async Task Constructor_ResponseAndPrefetched_StoresBoth()
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "Switching Protocols",
            StatusCode = 101,
            Version = "HTTP/1.1",
        };
        var responseData = new HypertextTransferProtocolResponseData(parameters);
        var responseExchange = new HypertextTransferProtocolProxyResponseExchange(
            ReadOnlyMemory<byte>.Empty,
            [],
            responseData);
        var prefetched = new byte[] { 0x81, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F };

        var exchange = new UpgradeResponseExchange(responseExchange, prefetched);

        await Assert.That(exchange.ResponseExchange).IsSameReferenceAs(responseExchange);
        await Assert.That(exchange.PrefetchedBytes).IsSameReferenceAs(prefetched);
    }
}
