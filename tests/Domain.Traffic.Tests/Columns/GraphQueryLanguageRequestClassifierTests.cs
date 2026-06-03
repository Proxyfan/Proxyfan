using System;
using System.Text;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Columns;

namespace Proxyfan.Domain.Traffic.Tests.Columns;

/// <summary>
///     Tests for <see cref="GraphQueryLanguageRequestClassifier" />.
/// </summary>
public sealed class GraphQueryLanguageRequestClassifierTests
{
    [Test]
    public async Task GetOperationName_GraphQlGetUrlQueryWithOperationName_ReturnsName()
    {
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/graphql?query=query%20Foo%20%7Bping%7D&operationName=Foo"),
            Version = "HTTP/1.1",
        });

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("Foo");
    }

    [Test]
    public async Task GetOperationName_GraphQlPostJsonWithOperationName_ReturnsName()
    {
        var request = BuildJsonPostRequest("https://example.com/graphql", "{\"operationName\":\"GetUser\",\"query\":\"query GetUser { user { id } }\"}");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("GetUser");
    }

    [Test]
    public async Task GetOperationName_GraphQlPostRaw_ReturnsExtractedName()
    {
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("query Foo { ping }"),
            Headers = HeaderCollection.Empty.Add("Content-Type", "application/graphql"),
            Method = "POST",
            RequestUri = new Uri("https://example.com/graphql"),
            Version = "HTTP/1.1",
        });

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("Foo");
    }

    [Test]
    public async Task GetOperationName_NonGraphQlRequest_ReturnsNull()
    {
        var request = BuildJsonPostRequest("https://example.com/api", "{}");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOperationName_PostJsonMalformed_ReturnsNull()
    {
        var request = BuildJsonPostRequest("https://example.com/graphql", "{not valid json");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsNull();
    }

    private static HypertextTransferProtocolRequestData BuildJsonPostRequest(string url, string body)
    {
        return new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes(body),
            Headers = HeaderCollection.Empty.Add("Content-Type", "application/json"),
            Method = "POST",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        });
    }
}
