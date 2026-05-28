using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="GraphQueryLanguageInspectorFormatter" />.
/// </summary>
public sealed class GraphQueryLanguageInspectorFormatterTests
{
    [Test]
    public async Task Format_NullRequest_ReturnsEmptyString()
    {
        var result = GraphQueryLanguageInspectorFormatter.Format(null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Format_NonGraphQlRequest_ReturnsEmptyString()
    {
        var request = BuildJsonPostRequest("https://example.com/api", "{}");

        var result = GraphQueryLanguageInspectorFormatter.Format(request);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Format_GraphQlPostJson_RendersOperationQueryAndVariables()
    {
        var body = "{\"operationName\":\"GetUser\",\"query\":\"query GetUser($id:ID!){user(id:$id){name}}\",\"variables\":{\"id\":\"42\"}}";
        var request = BuildJsonPostRequest("https://example.com/graphql", body);

        var result = GraphQueryLanguageInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Operation: GetUser")).IsTrue();
        await Assert.That(result.Contains("Query:")).IsTrue();
        await Assert.That(result.Contains("query GetUser")).IsTrue();
        await Assert.That(result.Contains("Variables:")).IsTrue();
        await Assert.That(result.Contains("\"id\"")).IsTrue();
    }

    [Test]
    public async Task Format_GraphQlPostJsonAnonymousOperation_RendersAnonymousLabel()
    {
        var body = "{\"query\":\"{ ping }\"}";
        var request = BuildJsonPostRequest("https://example.com/graphql", body);

        var result = GraphQueryLanguageInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Operation: (anonymous)")).IsTrue();
        await Assert.That(result.Contains("{ ping }")).IsTrue();
        await Assert.That(result.Contains("Variables:")).IsFalse();
    }

    [Test]
    public async Task Format_GraphQlRawApplicationGraphQl_RendersQueryOnly()
    {
        var body = "query Foo { ping }";
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/graphql");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes(body),
            Headers = headers,
            Method = "POST",
            RequestUri = new Uri("https://example.com/graphql"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var result = GraphQueryLanguageInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Operation: Foo")).IsTrue();
        await Assert.That(result.Contains("query Foo { ping }")).IsTrue();
    }

    [Test]
    public async Task Format_GraphQlGetWithUrlQuery_RendersOperationAndQuery()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/graphql?query=query%20Foo%20%7B%20ping%20%7D&operationName=Foo"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var result = GraphQueryLanguageInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Operation: Foo")).IsTrue();
        await Assert.That(result.Contains("query Foo { ping }")).IsTrue();
    }

    [Test]
    public async Task Format_GraphQlMalformedJson_ReturnsDiagnosticMessage()
    {
        var body = "{not valid json";
        var request = BuildJsonPostRequest("https://example.com/graphql", body);

        var result = GraphQueryLanguageInspectorFormatter.Format(request);

        await Assert.That(result.Contains("could not be parsed")).IsTrue();
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
