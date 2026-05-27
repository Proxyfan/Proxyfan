using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Additional tests for <see cref="Client.Traffic.ViewModels.TrafficFlowViewModel" />
///     targeting branch coverage gaps (null request paths, null response paths).
/// </summary>
public sealed class TrafficFlowViewModelEdgeCaseTests
{
    /// <summary>
    ///     Verifies that constructing from a tunnel flow with no request uses placeholder values.
    /// </summary>
    [Test]
    public async Task Constructor_FromTunnelFlowWithoutRequest_UsesPlaceholders()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:443", DateTimeOffset.UtcNow);

        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(flow, 1);

        await Assert.That(viewModel.Host).IsEqualTo("(tunnel)");
        await Assert.That(viewModel.Method).IsEqualTo("CONNECT");
        await Assert.That(viewModel.PathAndQuery).IsEqualTo("/");
        await Assert.That(viewModel.Request).IsNull();
        await Assert.That(viewModel.Response).IsNull();
        await Assert.That(viewModel.BodySize).IsEqualTo(0L);
        await Assert.That(viewModel.StatusCode).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that constructing from a flow whose request has no Host header uses placeholder host.
    /// </summary>
    [Test]
    public async Task Constructor_FromRequestEventWithoutHostHeader_UsesPlaceholder()
    {
        var flowId = Guid.NewGuid();
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/api/test"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        var requestEvent = new Proxyfan.Domain.Traffic.Events.RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);

        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);

        await Assert.That(viewModel.Host).IsEqualTo("(tunnel)");
    }
}
