using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for <see cref="ScriptableRequest" />, <see cref="ScriptableResponse" />, and
///     <see cref="ScriptableProjector" />.
/// </summary>
public sealed class ScriptableProjectorTests
{
    /// <summary>
    ///     Verifies that <see cref="ScriptableRequest" /> initializes from a source request.
    /// </summary>
    [Test]
    public async Task ScriptableRequest_Constructor_CopiesFields()
    {
        var source = BuildSourceRequest();

        var view = new ScriptableRequest(source);

        await Assert.That(view.Method).IsEqualTo("POST");
        await Assert.That(view.Url).IsEqualTo("https://example.com/api");
        await Assert.That(view.Headers.Get("X-Custom")).IsEqualTo("1");
    }

    /// <summary>
    ///     Verifies that <see cref="ScriptableResponse" /> initializes from a source response.
    /// </summary>
    [Test]
    public async Task ScriptableResponse_Constructor_CopiesFields()
    {
        var source = BuildSourceResponse();

        var view = new ScriptableResponse(source);

        await Assert.That(view.StatusCode).IsEqualTo(404);
        await Assert.That(view.ReasonPhrase).IsEqualTo("Not Found");
        await Assert.That(view.Headers.Get("Content-Type")).IsEqualTo("text/plain");
    }

    /// <summary>
    ///     Verifies that <see cref="ScriptableProjector.Project(ScriptableRequest, HypertextTransferProtocolRequestData)" />
    ///     materializes a new request preserving body bytes and applying view mutations.
    /// </summary>
    [Test]
    public async Task Project_ScriptableRequest_MaterializesNewRequest()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Method = "PATCH";
        view.Url = "https://api.example.com/data";
        view.Headers.Set("X-Custom", "modified");

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.Method).IsEqualTo("PATCH");
        await Assert.That(built.RequestUri.ToString()).IsEqualTo("https://api.example.com/data");
        await Assert.That(built.Headers.Get("X-Custom")).IsEqualTo("modified");
        await Assert.That(built.Body.Length).IsEqualTo(source.Body.Length);
    }

    /// <summary>
    ///     Verifies that <see cref="ScriptableProjector.Project(ScriptableResponse, HypertextTransferProtocolResponseData)" />
    ///     materializes a new response preserving body bytes and applying view mutations.
    /// </summary>
    [Test]
    public async Task Project_ScriptableResponse_MaterializesNewResponse()
    {
        var source = BuildSourceResponse();
        var view = new ScriptableResponse(source);
        view.StatusCode = 200;
        view.ReasonPhrase = "OK";

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.StatusCode).IsEqualTo(200);
        await Assert.That(built.ReasonPhrase).IsEqualTo("OK");
        await Assert.That(built.Body.Length).IsEqualTo(source.Body.Length);
    }

    private static HypertextTransferProtocolRequestData BuildSourceRequest()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("payload"),
            Headers = HeaderCollection.Empty.Add("X-Custom", "1"),
            Method = "POST",
            RequestUri = new Uri("https://example.com/api"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData BuildSourceResponse()
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.UTF8.GetBytes("body"),
            Headers = HeaderCollection.Empty.Add("Content-Type", "text/plain"),
            ReasonPhrase = "Not Found",
            StatusCode = 404,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
