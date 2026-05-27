using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ComposerRequestSender" /> driven by a stub
///     <see cref="HttpMessageHandler" /> that captures the outbound request and returns a
///     canned response.
/// </summary>
public sealed class ComposerRequestSenderTests
{
    /// <summary>
    ///     Verifies that <c>SendAsync</c> dispatches the configured method, URI, and body.
    /// </summary>
    [Test]
    public async Task SendAsync_PostWithBody_ForwardsToHandler()
    {
        var stub = new RecordingHandler(HttpStatusCode.OK, "ok");
        using var client = new HttpClient(stub);
        var sender = new ComposerRequestSender(client);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("payload"),
            Headers = HeaderCollection.Empty.Add("X-Custom", "value"),
            Method = "POST",
            RequestUri = new Uri("https://example.com/api"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        await sender.SendAsync(request, CancellationToken.None);

        await Assert.That(stub.LastRequest!.Method.Method).IsEqualTo("POST");
        await Assert.That(stub.LastRequest.RequestUri!.ToString()).IsEqualTo("https://example.com/api");
        await Assert.That(stub.LastRequestBody).IsEqualTo("payload");
    }

    /// <summary>
    ///     Verifies that the response status, headers, and body are surfaced on the returned
    ///     response data.
    /// </summary>
    [Test]
    public async Task SendAsync_AcceptedResponse_ReturnsStatusAndBody()
    {
        var stub = new RecordingHandler(HttpStatusCode.Accepted, "{\"ok\":true}");
        stub.ResponseHeaders["X-Trace-Id"] = "abc-123";
        stub.ContentType = "application/json";
        using var client = new HttpClient(stub);
        var sender = new ComposerRequestSender(client);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var response = await sender.SendAsync(request, CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(202);
        await Assert.That(Encoding.UTF8.GetString(response.Body.Span)).IsEqualTo("{\"ok\":true}");
        await Assert.That(response.Headers.Get("X-Trace-Id")).IsEqualTo("abc-123");
        await Assert.That(response.Headers.Get("Content-Type")).IsEqualTo("application/json");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody;

        public string ContentType { get; set; } = "text/plain";

        public string? LastRequestBody { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public Dictionary<string, string> ResponseHeaders { get; } = new(StringComparer.Ordinal);

        public RecordingHandler(HttpStatusCode status, string responseBody)
        {
            _status = status;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var contentBytes = Encoding.UTF8.GetBytes(_responseBody);
            var content = new ByteArrayContent(contentBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
            var response = new HttpResponseMessage(_status)
            {
                Content = content,
            };

            foreach (var header in ResponseHeaders)
            {
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return response;
        }
    }
}
