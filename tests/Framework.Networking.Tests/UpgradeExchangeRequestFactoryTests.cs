using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="UpgradeExchangeRequestFactory" />.
/// </summary>
public sealed class UpgradeExchangeRequestFactoryTests
{
    /// <summary>
    ///     Verifies that <see cref="UpgradeExchangeRequestFactory.Create" /> wires each
    ///     constructor argument into the matching init property on the returned
    ///     <see cref="UpgradeExchangeRequest" />.
    /// </summary>
    [Test]
    public async Task Create_AllArguments_PopulatesRequest()
    {
        var connection = new StubProxyConnection();
        var effective = BuildRequestData();
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var requestExchange = new HypertextTransferProtocolProxyRequestExchange(ReadOnlyMemory<byte>.Empty, Array.Empty<byte>(), BuildRequestData());

        var built = UpgradeExchangeRequestFactory.Create(connection, effective, flow, requestExchange);

        await Assert.That(built.Connection).IsSameReferenceAs(connection);
        await Assert.That(built.EffectiveRequest).IsSameReferenceAs(effective);
        await Assert.That(built.Flow).IsSameReferenceAs(flow);
        await Assert.That(built.RequestExchange).IsSameReferenceAs(requestExchange);
    }

    private static HypertextTransferProtocolRequestData BuildRequestData()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        return new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        });
    }
}
