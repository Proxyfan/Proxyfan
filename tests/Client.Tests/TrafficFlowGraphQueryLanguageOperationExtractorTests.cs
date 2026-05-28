using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TrafficFlowGraphQueryLanguageOperationExtractor" />.
/// </summary>
public sealed class TrafficFlowGraphQueryLanguageOperationExtractorTests
{
    [Test]
    public async Task Extract_NullRequest_ReturnsNull()
    {
        var result = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(null);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Extract_NonGraphQlRequest_ReturnsNull()
    {
        var request = BuildJsonPostRequest("https://example.com/api", "{}");

        var result = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(request);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Extract_GraphQlPostJsonWithOperationName_ReturnsName()
    {
        var body = "{\"operationName\":\"GetUser\",\"query\":\"query GetUser { user { id } }\"}";
        var request = BuildJsonPostRequest("https://example.com/graphql", body);

        var result = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(request);

        await Assert.That(result).IsEqualTo("GetUser");
    }

    [Test]
    public async Task Extract_GraphQlPostJsonAnonymous_ReturnsNull()
    {
        var body = "{\"query\":\"{ viewer { id } }\"}";
        var request = BuildJsonPostRequest("https://example.com/graphql", body);

        var result = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(request);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Extract_GraphQlPostRaw_ReturnsExtractedName()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/graphql");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("query Foo { ping }"),
            Headers = headers,
            Method = "POST",
            RequestUri = new Uri("https://example.com/graphql"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var result = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(request);

        await Assert.That(result).IsEqualTo("Foo");
    }

    [Test]
    public async Task Extract_GraphQlGetUrlQuery_ReturnsName()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/graphql?query=query%20Foo%20%7Bping%7D&operationName=Foo"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var result = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(request);

        await Assert.That(result).IsEqualTo("Foo");
    }

    [Test]
    public async Task Extract_GraphQlMalformedBody_ReturnsNull()
    {
        var request = BuildJsonPostRequest("https://example.com/graphql", "{not valid json");

        var result = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(request);

        await Assert.That(result).IsNull();
    }

    private static HypertextTransferProtocolRequestData BuildJsonPostRequest(string url, string body)
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/json");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes(body),
            Headers = headers,
            Method = "POST",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
