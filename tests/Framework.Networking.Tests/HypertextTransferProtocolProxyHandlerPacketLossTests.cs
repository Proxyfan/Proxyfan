using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Throttling;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Packet-loss tests for <see cref="HypertextTransferProtocolProxyHandler" />. The
///     handler consults <see cref="ThrottleApplier.HasPacketLossOccurred" /> at the very
///     start of every exchange; when the sampler indicates a drop the flow is marked failed,
///     added to the traffic store, and the upstream connection is never attempted.
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolProxyHandlerPacketLossTests
{
    /// <summary>
    ///     When the throttle profile prescribes 100% packet loss the handler drops the
    ///     exchange, never writes any bytes back to the client, and persists a failed flow.
    /// </summary>
    [Test]
    public async Task HandleAsync_PacketLossDropsExchange_FailsFlowAndWritesNothing()
    {
        var trafficStore = new StubTrafficStore();
        var throttleProfile = CreateThrottleProfile(1.0);
        var handler = CreateHandler(trafficStore, CreateEmptyRuleEngine(), throttleProfile, () => 0.0);

        var connection = new StubFullDuplexProxyConnection();
        var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: example.com\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        var responseBytes = await connection.ReadAllOutputAsync();

        await Assert.That(responseBytes.Length).IsEqualTo(0);
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        var storedFlow = GetFirstFlow(trafficStore);
        await Assert.That(storedFlow.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     When the sampler reports a value above the loss probability the handler
    ///     processes the request normally. Here a Block rule short-circuits the request to
    ///     a 403 so the test does not require a live upstream.
    /// </summary>
    [Test]
    public async Task HandleAsync_PacketLossSamplerAboveThreshold_ProcessesRequestNormally()
    {
        var trafficStore = new StubTrafficStore();
        var throttleProfile = CreateThrottleProfile(0.1);
        var handler = CreateHandler(trafficStore, CreateBlockAllRuleEngine(), throttleProfile, () => 0.9);

        var connection = new StubFullDuplexProxyConnection();
        var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: example.com\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.StartsWith("HTTP/1.1 403", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     When no throttle profile is configured the handler does not invoke the sampler at
    ///     all. This guards against the regression where the sampler short-circuits all
    ///     traffic when no profile is in effect.
    /// </summary>
    [Test]
    public async Task HandleAsync_PacketLossSamplerWithoutProfile_DoesNotInvokeSampler()
    {
        var trafficStore = new StubTrafficStore();
        var samplerInvocations = 0;
        PacketLossSampler counter = () =>
        {
            samplerInvocations++;
            return 0.0;
        };
        var handler = CreateHandler(trafficStore, CreateBlockAllRuleEngine(), throttleProfile: null, counter);

        var connection = new StubFullDuplexProxyConnection();
        var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: example.com\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        await Assert.That(samplerInvocations).IsEqualTo(0);
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     The handler must accept dependencies that omit the optional sampler and fall back
    ///     to the shared default sampler without throwing.
    /// </summary>
    [Test]
    public async Task HandleAsync_DefaultPacketLossSampler_ProcessesRequestNormally()
    {
        var trafficStore = new StubTrafficStore();
        var handler = CreateHandler(trafficStore, CreateBlockAllRuleEngine(), throttleProfile: null, packetLossSampler: null);

        var connection = new StubFullDuplexProxyConnection();
        var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: example.com\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    private static HypertextTransferProtocolProxyHandler CreateHandler(
        StubTrafficStore trafficStore,
        RuleEngine ruleEngine,
        MutableThrottleProfile? throttleProfile,
        PacketLossSampler? packetLossSampler)
    {
        var dependencies = new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = trafficStore,
            EventBus = new StubDomainEventBus(),
            RuleEngine = ruleEngine,
            Logger = NullLogger<HypertextTransferProtocolProxyHandler>.Instance,
            ThrottleProfile = throttleProfile,
            PacketLossSampler = packetLossSampler,
        };
        var handler = new HypertextTransferProtocolProxyHandler(dependencies);
        return handler;
    }

    private static RuleEngine CreateBlockAllRuleEngine()
    {
        var matchingRule = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var blockList = new BlockListRule(new[] { matchingRule }, isEnabled: true, priority: 0);
        IRequestPhaseRule[] requestRules = { blockList };
        var engine = new RuleEngine(requestRules, Array.Empty<IResponsePhaseRule>());
        return engine;
    }

    private static RuleEngine CreateEmptyRuleEngine()
    {
        var engine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>());
        return engine;
    }

    private static MutableThrottleProfile CreateThrottleProfile(double packetLossProbability)
    {
        var holder = new MutableThrottleProfile();
        var parameters = new ThrottleProfileParameters
        {
            DownloadBytesPerSecond = 1024,
            UploadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = packetLossProbability,
        };
        holder.SetProfile(new ThrottleProfile("Test", parameters));
        return holder;
    }

    private static TrafficFlow GetFirstFlow(StubTrafficStore trafficStore)
    {
        IReadOnlyList<TrafficFlow> flows = trafficStore.GetAll();
        return flows[0];
    }
}

