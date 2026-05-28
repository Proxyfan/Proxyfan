using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Provisioning tests for <see cref="HypertextTransferProtocolProxyHandler" /> covering the
///     certificate-provisioning short-circuit when a request targets the magic
///     <c>proxyfan.proxy</c> host with a configured certificate-authority provider.
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolProxyHandlerProvisioningTests
{
    /// <summary>
    ///     A GET to the magic provisioning host with a configured certificate-authority provider
    ///     responds with the certificate provisioning payload (200) and does not attempt any
    ///     upstream connection.
    /// </summary>
    [Test]
    public async Task HandleAsync_ProvisioningHostWithAuthority_RespondsWithCertificatePayload()
    {
        var generator = new StubCertificateGenerator();
        var authorityProvider = new MutableCertificateAuthorityProvider(generator);
        var trafficStore = new StubTrafficStore();
        var handler = new HypertextTransferProtocolProxyHandler(new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = trafficStore,
            EventBus = new StubDomainEventBus(),
            RuleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), Array.Empty<IResponsePhaseRule>()),
            Logger = NullLogger<HypertextTransferProtocolProxyHandler>.Instance,
            CertificateAuthorityProvider = authorityProvider,
        });

        var connection = new StubFullDuplexProxyConnection();
        var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: proxyfan.proxy\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }
}
