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
    public async Task GetOperationName_NullRequest_ReturnsNull()
    {
        var result = GraphQueryLanguageRequestClassifier.GetOperationName(null);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOperationName_NonGraphQlRequest_ReturnsNull()
    {
        var request = BuildJsonPostRequest("https://example.com/api", "{}");

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOperationName_GraphQlPostJsonWithOperationName_ReturnsName()
    {
        var body = "{\"operationName\":\"GetUser\",\"query\":\"query GetUser { user { id } }\"}";
        var request = BuildJsonPostRequest("https://example.com/graphql", body);

        var result = GraphQueryLanguageRequestClassifier.GetOperationName(request);

        await Assert.That(result).IsEqualTo("GetUser");
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
