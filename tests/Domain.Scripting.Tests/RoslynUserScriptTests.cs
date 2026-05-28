using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for <see cref="RoslynUserScript" /> that exercise the null-script short-circuit
///     paths in <c>OnRequestAsync</c> and <c>OnResponseAsync</c>.
/// </summary>
public sealed class RoslynUserScriptTests
{
    /// <summary>
    ///     A script constructed with null request and response scripts reports both phases as
    ///     disabled.
    /// </summary>
    [Test]
    public async Task Constructor_BothPhasesNull_DisablesBothPhases()
    {
        var script = new RoslynUserScript("disabled", requestScript: null, responseScript: null);

        await Assert.That(script.IsRequestPhaseEnabled).IsFalse();
        await Assert.That(script.IsResponsePhaseEnabled).IsFalse();
        await Assert.That(script.DisplayName).IsEqualTo("disabled");
    }

    /// <summary>
    ///     <see cref="RoslynUserScript.OnRequestAsync" /> is a no-op when the request script is
    ///     null. Exercises the null-script short-circuit branch.
    /// </summary>
    [Test]
    public async Task OnRequestAsync_NullRequestScript_CompletesWithoutThrowing()
    {
        var script = new RoslynUserScript("noop", requestScript: null, responseScript: null);
        var request = BuildScriptableRequest();
        var sharedState = new Dictionary<string, object?>();

        await script.OnRequestAsync(request, sharedState, CancellationToken.None);

        await Assert.That(sharedState.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     <see cref="RoslynUserScript.OnResponseAsync" /> is a no-op when the response script
    ///     is null. Exercises the null-script short-circuit branch.
    /// </summary>
    [Test]
    public async Task OnResponseAsync_NullResponseScript_CompletesWithoutThrowing()
    {
        var script = new RoslynUserScript("noop", requestScript: null, responseScript: null);
        var request = BuildScriptableRequest();
        var response = BuildScriptableResponse();
        var sharedState = new Dictionary<string, object?>();

        await script.OnResponseAsync(request, response, sharedState, CancellationToken.None);

        await Assert.That(sharedState.Count).IsEqualTo(0);
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
