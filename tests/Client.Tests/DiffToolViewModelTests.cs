using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Diff;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="DiffToolViewModel" />.
/// </summary>
public sealed class DiffToolViewModelTests
{
    /// <summary>
    ///     After construction, the view model exposes the current pool.
    /// </summary>
    [Test]
    public async Task Constructor_PreexistingFlows_PopulatesCollection()
    {
        var pool = new TrafficFlowDiffPool();
        pool.Add(BuildFlow("GET", "https://example.com/one", 200));
        pool.Add(BuildFlow("GET", "https://example.com/two", 200));

        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(2);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Selecting matching left and right flows produces a diff text.
    /// </summary>
    [Test]
    public async Task SelectingLeftAndRight_DifferentFlows_ProducesDiffText()
    {
        var left = BuildFlow("GET", "https://example.com/one", 200);
        var right = BuildFlow("POST", "https://example.com/one", 201);
        var pool = new TrafficFlowDiffPool();
        pool.Add(left);
        pool.Add(right);
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);

        viewModel.LeftFlow = viewModel.Flows[0];
        viewModel.RightFlow = viewModel.Flows[1];

        await Assert.That(viewModel.DiffText).IsNotEmpty();
        await Assert.That(viewModel.IsIdentical).IsFalse();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Selecting the same flow on both sides reports identical and renders no diff.
    /// </summary>
    [Test]
    public async Task SelectingSameLeftAndRight_IdenticalFlow_ReportsIdentical()
    {
        var flow = BuildFlow("GET", "https://example.com/one", 200);
        var pool = new TrafficFlowDiffPool();
        pool.Add(flow);
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);

        viewModel.LeftFlow = viewModel.Flows[0];
        viewModel.RightFlow = viewModel.Flows[0];

        await Assert.That(viewModel.IsIdentical).IsTrue();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Clearing the pool empties the Flows collection and resets selections.
    /// </summary>
    [Test]
    public async Task ClearCommand_WithExistingFlows_EmptiesPool()
    {
        var pool = new TrafficFlowDiffPool();
        pool.Add(BuildFlow("GET", "https://example.com/one", 200));
        pool.Add(BuildFlow("GET", "https://example.com/two", 200));
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);
        viewModel.LeftFlow = viewModel.Flows[0];
        viewModel.RightFlow = viewModel.Flows[1];

        viewModel.ClearCommand.Execute(null);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
        await Assert.That(viewModel.LeftFlow).IsNull();
        await Assert.That(viewModel.RightFlow).IsNull();
        await Assert.That(viewModel.DiffText).IsEmpty();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Removing a flow via the command removes the corresponding row.
    /// </summary>
    [Test]
    public async Task RemoveCommand_WithItem_RemovesFromPool()
    {
        var pool = new TrafficFlowDiffPool();
        var flow = BuildFlow("GET", "https://example.com/one", 200);
        pool.Add(flow);
        pool.Add(BuildFlow("GET", "https://example.com/two", 200));
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);
        var firstItem = viewModel.Flows[0];

        viewModel.RemoveCommand.Execute(firstItem);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(1);
        await Assert.That(pool.Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Removing the currently selected flow clears the selection.
    /// </summary>
    [Test]
    public async Task RemoveCommand_WithSelectedItem_ClearsSelection()
    {
        var pool = new TrafficFlowDiffPool();
        var flow = BuildFlow("GET", "https://example.com/one", 200);
        pool.Add(flow);
        pool.Add(BuildFlow("GET", "https://example.com/two", 200));
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);
        viewModel.LeftFlow = viewModel.Flows[0];
        viewModel.RightFlow = viewModel.Flows[1];

        viewModel.RemoveCommand.Execute(viewModel.LeftFlow);

        await Assert.That(viewModel.LeftFlow).IsNull();
        await Assert.That(viewModel.RightFlow).IsNotNull();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Remove with a null argument is a safe no-op.
    /// </summary>
    [Test]
    public async Task RemoveCommand_WithNull_DoesNothing()
    {
        var pool = new TrafficFlowDiffPool();
        pool.Add(BuildFlow("GET", "https://example.com/one", 200));
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);

        viewModel.RemoveCommand.Execute(null);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Disposing unsubscribes from the pool so later mutations do not refresh the VM.
    /// </summary>
    [Test]
    public async Task Dispose_AfterPoolChange_StopsReacting()
    {
        var pool = new TrafficFlowDiffPool();
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);
        viewModel.Dispose();

        pool.Add(BuildFlow("GET", "https://example.com/one", 200));

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Selecting only the left flow leaves the diff empty.
    /// </summary>
    [Test]
    public async Task SelectingOnlyLeft_WithoutRight_LeavesDiffEmpty()
    {
        var pool = new TrafficFlowDiffPool();
        pool.Add(BuildFlow("GET", "https://example.com/one", 200));
        var viewModel = new DiffToolViewModel(pool, InlineUserInterfaceScheduler.Instance);

        viewModel.LeftFlow = viewModel.Flows[0];

        await Assert.That(viewModel.DiffText).IsEmpty();
        await Assert.That(viewModel.IsIdentical).IsFalse();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Item view models project the flow's method, URL, and status.
    /// </summary>
    [Test]
    public async Task DiffPoolItem_BuiltFromFlow_DisplaysMethodAndUrl()
    {
        var flow = BuildFlow("PUT", "https://example.com/api", 204);
        var item = new DiffPoolItemViewModel(flow);

        await Assert.That(item.DisplayName).Contains("PUT");
        await Assert.That(item.DisplayName).Contains("https://example.com/api");
        await Assert.That(item.DisplayName).Contains("204");
    }

    private static TrafficFlow BuildFlow(string method, string url, int statusCode)
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:8080", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        });
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        flow.SetResponse(response);
        return flow;
    }
}
