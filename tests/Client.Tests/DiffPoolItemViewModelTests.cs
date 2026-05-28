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
///     Tests for <see cref="DiffPoolItemViewModel" />.
/// </summary>
public sealed class DiffPoolItemViewModelTests
{
    [Test]
    public async Task Constructor_FromFlow_ExposesUnderlyingFlowAndTimestamp()
    {
        var startedAt = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:0", startedAt);

        var viewModel = new DiffPoolItemViewModel(flow);

        await Assert.That(viewModel.Flow).IsSameReferenceAs(flow);
        await Assert.That(viewModel.StartedAt).IsEqualTo(startedAt);
    }

    [Test]
    public async Task Constructor_FromCompleteFlow_BuildsDisplayName()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:0", DateTimeOffset.UtcNow);
        var requestParams = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.test/"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParams));

        var viewModel = new DiffPoolItemViewModel(flow);

        await Assert.That(viewModel.DisplayName).IsEqualTo("GET https://example.test/");
    }
}
