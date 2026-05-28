using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="BreakpointPauseViewModel" />.
/// </summary>
public sealed class BreakpointPauseViewModelTests
{
    [Test]
    public async Task Constructor_RequestPhase_PopulatesEditableFields()
    {
        var request = BuildRequest("POST", "https://example.com/api", "body-data", "X-Test", "v1");
        var pause = new BreakpointPause(Guid.NewGuid(), request);

        var viewModel = new BreakpointPauseViewModel(pause);

        await Assert.That(viewModel.Phase).IsEqualTo(BreakpointPhase.Request);
        await Assert.That(viewModel.Method).IsEqualTo("POST");
        await Assert.That(viewModel.RequestUrl).IsEqualTo("https://example.com/api");
        await Assert.That(viewModel.BodyText).IsEqualTo("body-data");
        await Assert.That(viewModel.HeadersText.Contains("X-Test: v1")).IsTrue();
        await Assert.That(viewModel.StatusCode).IsEqualTo(0);
        await Assert.That(viewModel.ReasonPhrase).IsEqualTo(string.Empty);
        await Assert.That(viewModel.Pause).IsSameReferenceAs(pause);
    }

    [Test]
    public async Task Constructor_ResponsePhase_PopulatesStatusAndReason()
    {
        var request = BuildRequest("GET", "https://example.com/", string.Empty);
        var response = BuildResponse(404, "Not Found", "missing", "X-Cache", "MISS");
        var pause = new BreakpointPause(Guid.NewGuid(), request, response);

        var viewModel = new BreakpointPauseViewModel(pause);

        await Assert.That(viewModel.Phase).IsEqualTo(BreakpointPhase.Response);
        await Assert.That(viewModel.StatusCode).IsEqualTo(404);
        await Assert.That(viewModel.ReasonPhrase).IsEqualTo("Not Found");
        await Assert.That(viewModel.BodyText).IsEqualTo("missing");
        await Assert.That(viewModel.HeadersText.Contains("X-Cache: MISS")).IsTrue();
    }

    [Test]
    public async Task BuildRequestDecision_AfterEdit_ProducesModifiedRequest()
    {
        var request = BuildRequest("GET", "https://example.com/", string.Empty);
        var pause = new BreakpointPause(Guid.NewGuid(), request);
        var viewModel = new BreakpointPauseViewModel(pause);
        viewModel.Method = "POST";
        viewModel.RequestUrl = "https://example.com/edited";
        viewModel.HeadersText = "Content-Type: application/json";
        viewModel.BodyText = "{\"hi\":1}";

        var decision = viewModel.BuildRequestDecision();

        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedRequest).IsNotNull();
        await Assert.That(decision.ModifiedRequest!.Method).IsEqualTo("POST");
        await Assert.That(decision.ModifiedRequest.RequestUri.AbsoluteUri).IsEqualTo("https://example.com/edited");
        await Assert.That(decision.ModifiedRequest.Headers.Get("Content-Type")).IsEqualTo("application/json");
        await Assert.That(Encoding.UTF8.GetString(decision.ModifiedRequest.Body.Span)).IsEqualTo("{\"hi\":1}");
        await Assert.That(decision.ModifiedRequest.Version).IsEqualTo("HTTP/1.1");
    }

    [Test]
    public async Task BuildResponseDecision_AfterEdit_ProducesModifiedResponse()
    {
        var request = BuildRequest("GET", "https://example.com/", string.Empty);
        var response = BuildResponse(200, "OK", "original-body");
        var pause = new BreakpointPause(Guid.NewGuid(), request, response);
        var viewModel = new BreakpointPauseViewModel(pause);
        viewModel.StatusCode = 500;
        viewModel.ReasonPhrase = "Internal Server Error";
        viewModel.HeadersText = "X-Edit: yes";
        viewModel.BodyText = "edited body";

        var decision = viewModel.BuildResponseDecision();

        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedResponse).IsNotNull();
        await Assert.That(decision.ModifiedResponse!.StatusCode).IsEqualTo(500);
        await Assert.That(decision.ModifiedResponse.ReasonPhrase).IsEqualTo("Internal Server Error");
        await Assert.That(decision.ModifiedResponse.Headers.Get("X-Edit")).IsEqualTo("yes");
        await Assert.That(Encoding.UTF8.GetString(decision.ModifiedResponse.Body.Span)).IsEqualTo("edited body");
        await Assert.That(decision.ModifiedResponse.Version).IsEqualTo("HTTP/1.1");
    }

    private static HypertextTransferProtocolRequestData BuildRequest(
        string method,
        string url,
        string body,
        string? headerName = null,
        string? headerValue = null)
    {
        var headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host);
        if (headerName is not null && headerValue is not null)
        {
            headers = headers.Add(headerName, headerValue);
        }

        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes(body),
            Headers = headers,
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData BuildResponse(
        int statusCode,
        string reason,
        string body,
        string? headerName = null,
        string? headerValue = null)
    {
        var headers = HeaderCollection.Empty;
        if (headerName is not null && headerValue is not null)
        {
            headers = headers.Add(headerName, headerValue);
        }

        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.UTF8.GetBytes(body),
            Headers = headers,
            ReasonPhrase = reason,
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
