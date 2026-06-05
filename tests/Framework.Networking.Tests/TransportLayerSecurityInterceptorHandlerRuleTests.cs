using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Focused tests for request-phase rule handling inside
///     <see cref="TransportLayerSecurityInterceptorHandler" /> after TLS termination.
/// </summary>
[NotInParallel]
public sealed class TransportLayerSecurityInterceptorHandlerRuleTests
{
    /// <summary>
    ///     Verifies that a blocked intercepted request writes a synthetic 403 response, records
    ///     the flow, and does not write the request upstream.
    /// </summary>
    [Test]
    public async Task ProcessInterceptedExchangeAsync_BlockRule_Writes403AndSkipsUpstreamRequest()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { new BlockListRule([matching], isEnabled: true, priority: 0) }, Array.Empty<IResponsePhaseRule>());
        var handler = BuildHandler(ruleEngine, out var trafficStore);
        var pipes = BuildPipes(out var clientEgressReader, out var serverEgressReader);
        var loopContext = new TransportLayerSecurityInterceptedLoopContext
        {
            ClientSecureStream = Stream.Null,
            Connection = new StubProxyConnection(),
            Pipes = pipes,
            ServerSecureStream = Stream.Null,
        };
        var requestExchange = BuildRequestExchange(new Uri("https://blocked.example/path"));

        var canContinue = await InvokeProcessInterceptedExchangeAsync(handler, loopContext, requestExchange);
        var clientBytes = await ReadAvailableAsync(clientEgressReader);
        var serverBytes = await ReadAvailableAsync(serverEgressReader);
        var clientText = Encoding.ASCII.GetString(clientBytes);

        await Assert.That(canContinue).IsFalse();
        await Assert.That(clientText.StartsWith("HTTP/1.1 403", StringComparison.Ordinal)).IsTrue();
        await Assert.That(serverBytes.Length).IsEqualTo(0);
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].Response!.StatusCode).IsEqualTo(403);
    }

    /// <summary>
    ///     Verifies that a Map Local intercepted request serves the configured local response and
    ///     skips any upstream write.
    /// </summary>
    [Test]
    public async Task ProcessInterceptedExchangeAsync_MapLocalRule_WritesLocalResponseAndSkipsUpstreamRequest()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var parameters = new MapLocalRuleParameters
        {
            Body = Encoding.UTF8.GetBytes("hello local"),
            Headers =
            [
                new KeyValuePair<string, string>("Connection", "close"),
                new KeyValuePair<string, string>("Content-Length", "11"),
                new KeyValuePair<string, string>("Content-Type", "text/plain"),
            ],
            IsEnabled = true,
            Priority = 0,
            ReasonPhrase = "OK",
            StatusCode = 200,
        };
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { new MapLocalRule(matching, parameters) }, Array.Empty<IResponsePhaseRule>());
        var handler = BuildHandler(ruleEngine, out var trafficStore);
        var pipes = BuildPipes(out var clientEgressReader, out var serverEgressReader);
        var loopContext = new TransportLayerSecurityInterceptedLoopContext
        {
            ClientSecureStream = Stream.Null,
            Connection = new StubProxyConnection(),
            Pipes = pipes,
            ServerSecureStream = Stream.Null,
        };
        var requestExchange = BuildRequestExchange(new Uri("https://local.example/path"));

        _ = await InvokeProcessInterceptedExchangeAsync(handler, loopContext, requestExchange);
        var clientBytes = await ReadAvailableAsync(clientEgressReader);
        var serverBytes = await ReadAvailableAsync(serverEgressReader);
        var clientText = Encoding.ASCII.GetString(clientBytes);

        await Assert.That(clientText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
        await Assert.That(clientText.Contains("hello local", StringComparison.Ordinal)).IsTrue();
        await Assert.That(serverBytes.Length).IsEqualTo(0);
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].Response!.StatusCode).IsEqualTo(200);
    }

    private static TransportLayerSecurityInterceptorHandler BuildHandler(IRuleEngine ruleEngine, out StubTrafficStore trafficStore)
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: false);
        var context = new TransportLayerSecurityInterceptionContext(new MutableCertificateAuthorityProvider(new StubCertificateGenerator()), proxyingList);
        var newStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var handler = new TransportLayerSecurityInterceptorHandler(new TransportLayerSecurityInterceptorHandlerDependencies
        {
            Context = context,
            EventBus = eventBus,
            Logger = NullLogger<TransportLayerSecurityInterceptorHandler>.Instance,
            RuleEngine = ruleEngine,
            TrafficStore = newStore,
        });
        trafficStore = newStore;
        return handler;
    }

    private static TransportLayerSecurityInterceptionPipes BuildPipes(
        out PipeReader clientEgressReader,
        out PipeReader serverEgressReader)
    {
        var clientIngress = new Pipe();
        var clientEgress = new Pipe();
        var serverIngress = new Pipe();
        var serverEgress = new Pipe();
        clientEgressReader = clientEgress.Reader;
        serverEgressReader = serverEgress.Reader;
        return new TransportLayerSecurityInterceptionPipes(
            clientIngress.Reader,
            clientEgress.Writer,
            serverIngress.Reader,
            serverEgress.Writer);
    }

    private static HypertextTransferProtocolProxyRequestExchange BuildRequestExchange(Uri requestUri)
    {
        var headers = HeaderCollection.Empty.Add("Host", requestUri.Authority);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = requestUri,
            Version = "HTTP/1.1",
        });
        var headerText = $"GET {requestUri} HTTP/1.1\r\nHost: {requestUri.Authority}\r\n\r\n";
        return new HypertextTransferProtocolProxyRequestExchange(
            ReadOnlyMemory<byte>.Empty,
            Encoding.ASCII.GetBytes(headerText),
            request);
    }

    private static async Task<bool> InvokeProcessInterceptedExchangeAsync(
        TransportLayerSecurityInterceptorHandler handler,
        TransportLayerSecurityInterceptedLoopContext loopContext,
        HypertextTransferProtocolProxyRequestExchange requestExchange)
    {
        var method = typeof(TransportLayerSecurityInterceptorHandler).GetMethod(
            "ProcessInterceptedExchangeAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (Task<bool>)method!.Invoke(handler, [loopContext, requestExchange, CancellationToken.None])!;
        return await result;
    }

    private static async Task<byte[]> ReadAvailableAsync(PipeReader reader)
    {
        var collected = new List<byte>();
        while (true)
        {
            if (!reader.TryRead(out var result))
            {
                using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                try
                {
                    result = await reader.ReadAsync(cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            foreach (var segment in result.Buffer)
            {
                collected.AddRange(segment.ToArray());
            }

            reader.AdvanceTo(result.Buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }

        return collected.ToArray();
    }
}
