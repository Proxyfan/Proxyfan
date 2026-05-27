using Proxyfan.Domain.Scripting.Tests.Stubs;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for <see cref="UserScriptingHandler" />.
/// </summary>
public sealed class UserScriptingHandlerTests
{
    /// <summary>
    ///     Verifies that when scripting is disabled the request flows through unchanged.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_Disabled_ReturnsOriginal()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: false);
        configuration.SetActiveScript(new StubUserScript("script", (request, state) => request.Method = "PATCH"));
        var handler = new UserScriptingHandler(configuration);
        var source = BuildRequest("GET");

        var result = await handler.ApplyRequestAsync("flow", source, CancellationToken.None);

        await Assert.That(result.Method).IsEqualTo("GET");
    }

    /// <summary>
    ///     Verifies that when no script is configured the request flows through unchanged.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_NoActiveScript_ReturnsOriginal()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        var handler = new UserScriptingHandler(configuration);
        var source = BuildRequest("GET");

        var result = await handler.ApplyRequestAsync("flow", source, CancellationToken.None);

        await Assert.That(result.Method).IsEqualTo("GET");
    }

    /// <summary>
    ///     Verifies that a script without a request phase is bypassed for request hook.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_ResponseOnlyScript_ReturnsOriginal()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "script",
            onResponse: (req, resp, state) => resp.StatusCode = 999));
        var handler = new UserScriptingHandler(configuration);
        var source = BuildRequest("GET");

        var result = await handler.ApplyRequestAsync("flow", source, CancellationToken.None);

        await Assert.That(result.Method).IsEqualTo("GET");
    }

    /// <summary>
    ///     Verifies that a request-phase script mutation is projected back into a new request.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_WithMutation_ReturnsMutated()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "mutate",
            onRequest: (request, state) =>
            {
                request.Method = "PATCH";
                request.Headers.Set("X-Trace", "true");
            }));
        var handler = new UserScriptingHandler(configuration);
        var source = BuildRequest("GET");

        var result = await handler.ApplyRequestAsync("flow-1", source, CancellationToken.None);

        await Assert.That(result.Method).IsEqualTo("PATCH");
        await Assert.That(result.Headers.Get("X-Trace")).IsEqualTo("true");
    }

    /// <summary>
    ///     Verifies that when scripting is disabled the response flows through unchanged.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_Disabled_ReturnsOriginal()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: false);
        configuration.SetActiveScript(new StubUserScript(
            "script",
            onResponse: (req, resp, state) => resp.StatusCode = 999));
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");
        var sourceResponse = BuildResponse(200);

        var result = await handler.ApplyResponseAsync("flow", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(result.StatusCode).IsEqualTo(200);
    }

    /// <summary>
    ///     Verifies that response phase can read shared state written by the request phase
    ///     on the same flow id.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_SharedStateAcrossPhases_RoundTrips()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "shared",
            onRequest: (request, state) => state["correlation"] = "abc",
            onResponse: (req, resp, state) => resp.Headers.Set("X-Correlation", (string)(state["correlation"] ?? "missing"))));
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");
        var sourceResponse = BuildResponse(200);

        await handler.ApplyRequestAsync("flow-1", sourceRequest, CancellationToken.None);
        var result = await handler.ApplyResponseAsync("flow-1", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(result.Headers.Get("X-Correlation")).IsEqualTo("abc");
    }

    /// <summary>
    ///     Verifies that a response-phase script mutation is projected back into a new response.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_WithMutation_ReturnsMutated()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "mutate",
            onResponse: (req, resp, state) =>
            {
                resp.StatusCode = 201;
                resp.Headers.Set("X-Override", "yes");
            }));
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("POST");
        var sourceResponse = BuildResponse(200);

        var result = await handler.ApplyResponseAsync("flow-2", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(result.StatusCode).IsEqualTo(201);
        await Assert.That(result.Headers.Get("X-Override")).IsEqualTo("yes");
    }

    private static HypertextTransferProtocolRequestData BuildRequest(string method)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes(string.Empty),
            Headers = HeaderCollection.Empty,
            Method = method,
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData BuildResponse(int statusCode)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.UTF8.GetBytes(string.Empty),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
