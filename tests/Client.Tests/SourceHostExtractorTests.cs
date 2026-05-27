using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="SourceHostExtractor" />.
/// </summary>
public sealed class SourceHostExtractorTests
{
    /// <summary>
    ///     Verifies that the Host header is returned when present and non-blank.
    /// </summary>
    [Test]
    public async Task Extract_HostHeaderPresent_ReturnsHostHeader()
    {
        var headers = HeaderCollection.Empty.Add("Host", "header-host.test");
        var domainEvent = CreateRequest(headers, "https://uri-host.test/x");

        var result = SourceHostExtractor.Extract(domainEvent);

        await Assert.That(result).IsEqualTo("header-host.test");
    }

    /// <summary>
    ///     Verifies that the URI host is used when the Host header is missing.
    /// </summary>
    [Test]
    public async Task Extract_HostHeaderMissing_ReturnsUriHost()
    {
        var domainEvent = CreateRequest(HeaderCollection.Empty, "https://uri-host.test/x");

        var result = SourceHostExtractor.Extract(domainEvent);

        await Assert.That(result).IsEqualTo("uri-host.test");
    }

    /// <summary>
    ///     Verifies that the URI host is used when the Host header is blank.
    /// </summary>
    [Test]
    public async Task Extract_HostHeaderBlank_ReturnsUriHost()
    {
        var headers = HeaderCollection.Empty.Add("Host", "   ");
        var domainEvent = CreateRequest(headers, "https://uri-host.test/x");

        var result = SourceHostExtractor.Extract(domainEvent);

        await Assert.That(result).IsEqualTo("uri-host.test");
    }

    private static RequestReceived CreateRequest(HeaderCollection headers, string uri)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri(uri),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return new RequestReceived(Guid.NewGuid(), request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
    }
}
