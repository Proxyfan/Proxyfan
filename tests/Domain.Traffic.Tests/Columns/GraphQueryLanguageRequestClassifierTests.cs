using Proxyfan.Domain.Traffic.Columns;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests.Columns;

/// <summary>
///     Tests for <see cref="GraphQueryLanguageRequestClassifier" />.
/// </summary>
public sealed class GraphQueryLanguageRequestClassifierTests
{
    [Test]
    public async Task GetOperationName_GraphQlGetQuery_ReturnsName()
    {
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/graphql?query=query%20Foo%20%7B%20ping%20%7D&operationName=Foo"),
            Version = "HTTP/1.1",
        });

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("Foo");
    }

    [Test]
    public async Task GetOperationName_GraphQlJsonBody_ReturnsExplicitOperationName()
    {
        var request = BuildRequest("https://example.com/graphql", "POST", "application/json", "{\"operationName\":\"GetUser\",\"query\":\"query GetUser { user { id } }\"}");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("GetUser");
    }

    [Test]
    public async Task GetOperationName_GraphQlJsonBodyWithoutOperationName_ReturnsExtractedName()
    {
        var request = BuildRequest("https://example.com/graphql", "POST", "application/json", "{\"query\":\"query Viewer { viewer { id } }\"}");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("Viewer");
    }

    [Test]
    public async Task GetOperationName_GraphQlJsonMalformed_ReturnsNull()
    {
        var request = BuildRequest("https://example.com/graphql", "POST", "application/json", "{not valid json");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOperationName_GraphQlRawBody_ReturnsExtractedName()
    {
        var request = BuildRequest("https://example.com/graphql", "POST", "application/graphql", "query Foo { ping }");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("Foo");
    }

    [Test]
    public async Task GetOperationName_NonGraphQlRequest_ReturnsNull()
    {
        var request = BuildRequest("https://example.com/api", "POST", "application/json", "{\"query\":\"query Viewer { viewer { id } }\"}");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOperationName_NullRequest_ReturnsNull()
    {
        var result = GraphQueryLanguageRequestClassifier.GetOperationName(null);

        await Assert.That(result).IsNull();
    }

    private static HypertextTransferProtocolRequestData BuildRequest(string url, string method, string contentType, string body)
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", contentType);
        return new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes(body),
            Headers = headers,
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        });
    }
}
