using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="DiffPoolItemDisplayFormatter" />.
/// </summary>
public sealed class DiffPoolItemDisplayFormatterTests
{
    [Test]
    public async Task Format_FlowWithoutResponse_OmitsStatusSuffix()
    {
        var flow = CreateFlow(method: "GET", url: "https://example.com/path", responseStatus: null);

        var label = DiffPoolItemDisplayFormatter.Format(flow);

        await Assert.That(label).IsEqualTo("GET https://example.com/path");
    }

    [Test]
    public async Task Format_FlowWithResponse_IncludesArrowAndStatusCode()
    {
        var flow = CreateFlow(method: "POST", url: "https://api.example/x", responseStatus: 201);

        var label = DiffPoolItemDisplayFormatter.Format(flow);

        await Assert.That(label).IsEqualTo("POST https://api.example/x -> 201");
    }

    [Test]
    public async Task Format_FlowWithoutRequest_UsesPlaceholders()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:0", DateTimeOffset.UtcNow);

        var label = DiffPoolItemDisplayFormatter.Format(flow);

        await Assert.That(label).IsEqualTo("(no request) (no url)");
    }

    private static TrafficFlow CreateFlow(string method, string url, int? responseStatus)
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:0", DateTimeOffset.UtcNow);
        var requestParams = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParams));

        if (responseStatus is null)
        {
            return flow;
        }

        var responseParams = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = responseStatus.Value,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParams));
        return flow;
    }
}
