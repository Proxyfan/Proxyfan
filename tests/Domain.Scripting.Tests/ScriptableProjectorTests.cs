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
    ///     Verifies that <see cref="ScriptableRequest" /> preserves all values for multi-value
    ///     headers from the source request.
    /// </summary>
    [Test]
    public async Task ScriptableRequest_Constructor_PreservesMultiValueHeaders()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("payload"),
            Headers = HeaderCollection.Empty
                .Add("Set-Cookie", "a=1")
                .Add("Set-Cookie", "b=2"),
            Method = "GET",
            RequestUri = new Uri("https://example.com"),
            Version = "HTTP/1.1",
        };
        var source = new HypertextTransferProtocolRequestData(parameters);

        var view = new ScriptableRequest(source);

        var valueCount = 0;
        foreach (var header in view.Headers.Enumerate())
        {
            if (header.Key == "Set-Cookie")
            {
                valueCount++;
            }
        }

        await Assert.That(valueCount).IsEqualTo(2);
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
    ///     Verifies that <see cref="ScriptableResponse" /> preserves all values for multi-value
    ///     headers from the source response.
    /// </summary>
    [Test]
    public async Task ScriptableResponse_Constructor_PreservesMultiValueHeaders()
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.UTF8.GetBytes("body"),
            Headers = HeaderCollection.Empty
                .Add("Set-Cookie", "a=1")
                .Add("Set-Cookie", "b=2"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var source = new HypertextTransferProtocolResponseData(parameters);

        var view = new ScriptableResponse(source);

        var valueCount = 0;
        foreach (var header in view.Headers.Enumerate())
        {
            if (header.Key == "Set-Cookie")
            {
                valueCount++;
            }
        }

        await Assert.That(valueCount).IsEqualTo(2);
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

        await Assert.That(built.IsSuccess).IsTrue();
        await Assert.That(built.Value.Method).IsEqualTo("PATCH");
        await Assert.That(built.Value.RequestUri.ToString()).IsEqualTo("https://api.example.com/data");
        await Assert.That(built.Value.Headers.Get("X-Custom")).IsEqualTo("modified");
        await Assert.That(built.Value.Body.Length).IsEqualTo(source.Body.Length);
    }

    /// <summary>
    ///     Verifies that projection preserves all values from a multi-value request header.
    /// </summary>
    [Test]
    public async Task Project_ScriptableRequest_PreservesMultiValueHeaders()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("payload"),
            Headers = HeaderCollection.Empty
                .Add("Set-Cookie", "a=1")
                .Add("Set-Cookie", "b=2"),
            Method = "GET",
            RequestUri = new Uri("https://example.com"),
            Version = "HTTP/1.1",
        };
        var source = new HypertextTransferProtocolRequestData(parameters);
        var view = new ScriptableRequest(source);

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsTrue();
        await Assert.That(built.Value.Headers.GetAll("Set-Cookie").Length).IsEqualTo(2);
        await Assert.That(built.Value.Headers.GetAll("Set-Cookie")[0]).IsEqualTo("a=1");
        await Assert.That(built.Value.Headers.GetAll("Set-Cookie")[1]).IsEqualTo("b=2");
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

        await Assert.That(built.IsSuccess).IsTrue();
        await Assert.That(built.Value.StatusCode).IsEqualTo(200);
        await Assert.That(built.Value.ReasonPhrase).IsEqualTo("OK");
        await Assert.That(built.Value.Body.Length).IsEqualTo(source.Body.Length);
    }

    /// <summary>
    ///     Verifies that projecting a request with a non-absolute URL returns a typed failure
    ///     rather than throwing from the <see cref="Uri" /> constructor.
    /// </summary>
    [Test]
    public async Task Project_RequestWithRelativeUrl_ReturnsFailure()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Url = "/relative/path";

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_REQUEST_URL");
    }

    /// <summary>
    ///     Verifies that projecting a request with a blank method returns a typed failure.
    /// </summary>
    [Test]
    public async Task Project_RequestWithEmptyMethod_ReturnsFailure()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Method = string.Empty;

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_REQUEST_METHOD");
    }

    /// <summary>
    ///     Verifies that projecting a request with a method containing forbidden characters
    ///     (e.g. whitespace, which would corrupt the request line) returns a typed failure.
    /// </summary>
    [Test]
    public async Task Project_RequestWithMethodContainingWhitespace_ReturnsFailure()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Method = "GE T";

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_REQUEST_METHOD");
    }

    /// <summary>
    ///     Verifies that header values containing CR/LF (which would enable response-splitting)
    ///     are rejected as a typed failure rather than being copied into the projected request.
    /// </summary>
    [Test]
    public async Task Project_RequestWithHeaderValueContainingCarriageReturn_ReturnsFailure()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Headers.Set("X-Injected", "value\r\nX-Smuggled: yes");

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_HEADER_VALUE");
    }

    /// <summary>
    ///     Verifies that header names containing characters outside the HTTP token grammar
    ///     (e.g. spaces) are rejected as a typed failure.
    /// </summary>
    [Test]
    public async Task Project_RequestWithHeaderNameContainingInvalidCharacter_ReturnsFailure()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Headers.Set("Bad Header", "value");

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_HEADER_NAME");
    }

    /// <summary>
    ///     Verifies that projecting a response with a status code outside 100–999 returns a
    ///     typed failure rather than producing an out-of-range status line.
    /// </summary>
    [Test]
    public async Task Project_ResponseWithOutOfRangeStatusCode_ReturnsFailure()
    {
        var source = BuildSourceResponse();
        var view = new ScriptableResponse(source);
        view.StatusCode = 99;

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_RESPONSE_STATUS_CODE");
    }

    /// <summary>
    ///     Verifies that a reason phrase containing CR/LF is rejected as a typed failure to
    ///     prevent corruption of the status line.
    /// </summary>
    [Test]
    public async Task Project_ResponseWithReasonPhraseContainingNewline_ReturnsFailure()
    {
        var source = BuildSourceResponse();
        var view = new ScriptableResponse(source);
        view.ReasonPhrase = "OK\r\nX-Smuggled: yes";

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_RESPONSE_REASON_PHRASE");
    }

    /// <summary>
    ///     Verifies that header values containing C0 control bytes other than HTAB (here, a
    ///     literal <c>0x01</c>) are rejected per RFC 9110 §5.5 so they cannot reach the wire.
    /// </summary>
    [Test]
    public async Task Project_RequestWithHeaderValueContainingControlByte_ReturnsFailure()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Headers.Set("X-Bad", "value\u0001trailing");

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_HEADER_VALUE");
    }

    /// <summary>
    ///     Verifies that a URL whose scheme parses but whose authority is empty (e.g.
    ///     <c>http:/path</c>) is rejected, because it would produce an unusable request
    ///     target for the proxy pipeline.
    /// </summary>
    [Test]
    public async Task Project_RequestWithSchemeOnlyUrl_ReturnsFailure()
    {
        var source = BuildSourceRequest();
        var view = new ScriptableRequest(source);
        view.Url = "http:/path";

        var built = ScriptableProjector.Project(view, source);

        await Assert.That(built.IsSuccess).IsFalse();
        await Assert.That(built.Error!.Code).IsEqualTo("SCRIPT_INVALID_REQUEST_URL");
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
