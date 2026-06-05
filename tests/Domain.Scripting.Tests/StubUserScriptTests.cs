using Proxyfan.Domain.Scripting.Tests.Stubs;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Regression tests for <see cref="StubUserScript" />.
/// </summary>
public sealed class StubUserScriptTests
{
    [Test]
    public async Task OnRequestAsync_UnexpectedInvocation_ThrowsInvalidOperationException()
    {
        var script = new StubUserScript("response-only", onResponse: (request, response, state) => response.StatusCode = 201);
        var request = BuildScriptableRequest();
        var sharedState = new Dictionary<string, object?>();

        await Assert.That(async () => await script.OnRequestAsync(request, sharedState, CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("OnRequestAsync invoked on stub that was not configured for it.");
    }

    [Test]
    public async Task OnResponseAsync_UnexpectedInvocation_ThrowsInvalidOperationException()
    {
        var script = new StubUserScript("request-only", onRequest: (request, state) => request.Method = "PATCH");
        var request = BuildScriptableRequest();
        var response = BuildScriptableResponse();
        var sharedState = new Dictionary<string, object?>();

        await Assert.That(async () => await script.OnResponseAsync(request, response, sharedState, CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("OnResponseAsync invoked on stub that was not configured for it.");
    }

    private static ScriptableRequest BuildScriptableRequest()
    {
        var requestData = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        return new ScriptableRequest(requestData);
    }

    private static ScriptableResponse BuildScriptableResponse()
    {
        var responseData = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            StatusCode = 200,
            ReasonPhrase = "OK",
            Version = "HTTP/1.1",
        });
        return new ScriptableResponse(responseData);
    }
}
