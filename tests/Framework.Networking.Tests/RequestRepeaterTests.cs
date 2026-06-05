using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="RequestRepeater" /> verifying that captured requests are
///     replayed through the rule engine, the resulting flow is added to the store with
///     <see cref="TrafficFlowOrigin.Repeated" />, and the expected domain events are
///     published in order.
/// </summary>
public sealed class RequestRepeaterTests
{
    /// <summary>
    ///     Verifies that <see cref="RequestRepeater.RepeatAsync(HypertextTransferProtocolRequestData, CancellationToken)" />
    ///     sends the request, captures the response, and publishes the standard lifecycle events.
    /// </summary>
    [Test]
    public async Task RepeatAsync_NoRules_DispatchesAndCapturesFlow()
    {
        var sender = new StubComposerRequestSender();
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://example.com/data");

        var result = await repeater.RepeatAsync(request, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(sender.CapturedRequests.Count).IsEqualTo(1);
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        var storedFlow = trafficStore.AddedFlows[0];
        await Assert.That(storedFlow.Id).IsEqualTo(result.Value);
        await Assert.That(storedFlow.Origin).IsEqualTo(TrafficFlowOrigin.Repeated);
        await Assert.That(storedFlow.Status).IsEqualTo(TrafficFlowStatus.Complete);
        await Assert.That(storedFlow.Request!.Method).IsEqualTo("GET");
        await Assert.That(eventBus.PublishedOf<RequestReceived>().Any()).IsTrue();
        await Assert.That(eventBus.PublishedOf<ResponseReceived>().Any()).IsTrue();
        await Assert.That(eventBus.PublishedOf<TrafficFlowCompleted>().Any()).IsTrue();
    }

    /// <summary>
    ///     Verifies that when a BlockList rule matches, the repeater does not call the
    ///     upstream sender and the stored flow is marked complete with a synthetic 403.
    /// </summary>
    [Test]
    public async Task RepeatAsync_BlockListMatches_DoesNotCallSender()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var blockList = new BlockListRule(new[] { matching }, isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { blockList }, Array.Empty<IResponsePhaseRule>());
        var sender = new StubComposerRequestSender();
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://blocked.example/");

        await repeater.RepeatAsync(request, CancellationToken.None);

        await Assert.That(sender.CapturedRequests.Count).IsEqualTo(0);
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].Response!.StatusCode).IsEqualTo(403);
    }

    /// <summary>
    ///     Verifies that a MapLocal rule yields the local response and bypasses the sender.
    /// </summary>
    [Test]
    public async Task RepeatAsync_MapLocalMatches_ServesLocalResponseWithoutSender()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var parameters = new MapLocalRuleParameters
        {
            Body = Encoding.UTF8.GetBytes("local"),
            Headers = new[] { new KeyValuePair<string, string>("Content-Type", "text/plain") },
            IsEnabled = true,
            Priority = 0,
            ReasonPhrase = "OK",
            StatusCode = 201,
        };
        var mapLocal = new MapLocalRule(matching, parameters);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { mapLocal }, Array.Empty<IResponsePhaseRule>());
        var sender = new StubComposerRequestSender();
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://anything.example/");

        await repeater.RepeatAsync(request, CancellationToken.None);

        await Assert.That(sender.CapturedRequests.Count).IsEqualTo(0);
        await Assert.That(trafficStore.AddedFlows[0].Response!.StatusCode).IsEqualTo(201);
    }

    /// <summary>
    ///     Verifies that the multi-repeat overload sends the request N times.
    /// </summary>
    [Test]
    public async Task RepeatAsync_ThreeTimes_DispatchesThreeFlows()
    {
        var sender = new StubComposerRequestSender();
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("POST", "https://example.com/api");

        var result = await repeater.RepeatAsync(request, repeatCount: 3, TimeSpan.Zero, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.CompletedCount).IsEqualTo(3);
        await Assert.That(result.Value.RequestedCount).IsEqualTo(3);
        await Assert.That(sender.CapturedRequests.Count).IsEqualTo(3);
        await Assert.That(trafficStore.Count).IsEqualTo(3);
        await Assert.That(eventBus.PublishedOf<RequestReceived>().Count()).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that an upstream send failure result fails the repeated flow (Failed status) instead of
    ///     synthesising a blocked response, and skips response-phase rule evaluation and the
    ///     <see cref="ResponseReceived" /> event. The failed flow is still stored and a
    ///     <see cref="TrafficFlowCompleted" /> event is published so partial repeat sequences
    ///     can continue.
    /// </summary>
    [Test]
    public async Task RepeatAsync_SenderFailure_RecordsFailedFlowWithoutResponse()
    {
        var sender = new StubComposerRequestSender
        {
            ErrorToReturn = new ComposerSendError("upstream down", new InvalidOperationException("upstream down")),
        };
        var responseRule = new RecordingResponsePhaseRule();
        var ruleEngine = new RuleEngine(
            Array.Empty<IRequestPhaseRule>(),
            new IResponsePhaseRule[] { responseRule });
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://example.com/");

        var result = await repeater.RepeatAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.Count).IsEqualTo(1);
        var storedFlow = trafficStore.AddedFlows[0];
        await Assert.That(storedFlow.Status).IsEqualTo(TrafficFlowStatus.Failed);
        await Assert.That(storedFlow.Response).IsNull();
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<RequestReplayError>();
        await Assert.That(result.Error!.Code).IsEqualTo(RequestReplayError.DispatchFailedCode);
        await Assert.That(responseRule.EvaluationCount).IsEqualTo(0);
        await Assert.That(eventBus.PublishedOf<ResponseReceived>().Any()).IsFalse();
        var completed = eventBus.PublishedOf<TrafficFlowCompleted>().Single();
        await Assert.That(completed.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     Verifies that an invalid repeat count is rejected.
    /// </summary>
    [Test]
    public async Task RepeatAsync_ZeroCount_ReturnsDomainValidationError()
    {
        var sender = new StubComposerRequestSender();
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://example.com/");

        var result = await repeater.RepeatAsync(request, repeatCount: 0, TimeSpan.Zero, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<RequestReplayError>();
        await Assert.That(result.Error!.Code).IsEqualTo(RequestReplayError.InvalidRepeatCountCode);
    }

    /// <summary>
    ///     Verifies that a negative delay is rejected.
    /// </summary>
    [Test]
    public async Task RepeatAsync_NegativeDelay_ReturnsDomainValidationError()
    {
        var sender = new StubComposerRequestSender();
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://example.com/");

        var result = await repeater.RepeatAsync(request, repeatCount: 1, TimeSpan.FromSeconds(-1), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<RequestReplayError>();
        await Assert.That(result.Error!.Code).IsEqualTo(RequestReplayError.InvalidDelayCode);
    }

    /// <summary>
    ///     Verifies that a positive inter-repeat delay is honored between iterations
    ///     (covers the <c>Task.Delay</c> branch of the loop).
    /// </summary>
    [Test]
    public async Task RepeatAsync_TwoRepeatsWithPositiveDelay_DelaysBetweenIterations()
    {
        var sender = new StubComposerRequestSender();
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://example.com/delay");

        var result = await repeater.RepeatAsync(request, repeatCount: 2, TimeSpan.FromMilliseconds(1), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.CompletedCount).IsEqualTo(2);
        await Assert.That(trafficStore.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that cancellation is reported through a replay domain error rather than
    ///     escaping as an exception.
    /// </summary>
    [Test]
    public async Task RepeatAsync_CancelledToken_ReturnsCancellationError()
    {
        var sender = new StubComposerRequestSender
        {
            ExceptionToThrow = new OperationCanceledException("cancelled"),
        };
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://example.com/");
        var result = await repeater.RepeatAsync(request, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<RequestReplayError>();
        await Assert.That(result.Error!.Code).IsEqualTo(RequestReplayError.CancelledCode);
    }

    /// <summary>
    ///     Verifies that the multi-repeat overload reports partial progress when a replay fails.
    /// </summary>
    [Test]
    public async Task RepeatAsync_SecondIterationFails_ReturnsFailureWithCompletedCount()
    {
        var sender = new StubComposerRequestSender();
        sender.ResponseFactory = (_, requestNumber) =>
        {
            if (requestNumber == 2)
            {
                throw new InvalidOperationException("second failed");
            }

            return BuildResponse(200, "OK");
        };
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var repeater = new RequestRepeater(sender, ruleEngine, trafficStore, eventBus, TimeProvider.System);
        var request = BuildRequest("GET", "https://example.com/");

        var result = await repeater.RepeatAsync(request, repeatCount: 3, TimeSpan.Zero, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<RequestReplayError>();
        var replayError = (RequestReplayError)result.Error!;
        await Assert.That(replayError.CompletedCount).IsEqualTo(1);
        await Assert.That(replayError.Code).IsEqualTo(RequestReplayError.DispatchFailedCode);
    }

    private static HypertextTransferProtocolRequestData BuildRequest(string method, string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return request;
    }

    private static HypertextTransferProtocolResponseData BuildResponse(int statusCode, string reasonPhrase)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = reasonPhrase,
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        return response;
    }
}
