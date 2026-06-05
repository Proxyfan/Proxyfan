using Proxyfan.Domain.Scripting.Tests.Stubs;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
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

        var outcome = await handler.ApplyRequestAsync("flow", source, CancellationToken.None);

        await Assert.That(outcome.Value.Method).IsEqualTo("GET");
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

        var outcome = await handler.ApplyRequestAsync("flow", source, CancellationToken.None);

        await Assert.That(outcome.Value.Method).IsEqualTo("GET");
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

        var outcome = await handler.ApplyRequestAsync("flow", source, CancellationToken.None);

        await Assert.That(outcome.Value.Method).IsEqualTo("GET");
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

        var outcome = await handler.ApplyRequestAsync("flow-1", source, CancellationToken.None);

        await Assert.That(outcome.Value.Method).IsEqualTo("PATCH");
        await Assert.That(outcome.Value.Headers.Get("X-Trace")).IsEqualTo("true");
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

        var outcome = await handler.ApplyResponseAsync("flow", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(outcome.Value.StatusCode).IsEqualTo(200);
    }

    /// <summary>
    ///     Verifies that when no script is configured the response flows through unchanged.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_NoActiveScript_ReturnsOriginal()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");
        var sourceResponse = BuildResponse(200);

        var outcome = await handler.ApplyResponseAsync("flow", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(outcome.Value.StatusCode).IsEqualTo(200);
    }

    /// <summary>
    ///     Verifies that a script that only opts in to the request phase is bypassed for the
    ///     response hook.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_RequestOnlyScript_ReturnsOriginal()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "script",
            onRequest: (request, state) => request.Method = "PATCH"));
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");
        var sourceResponse = BuildResponse(200);

        var outcome = await handler.ApplyResponseAsync("flow", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(outcome.Value.StatusCode).IsEqualTo(200);
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
        var outcome = await handler.ApplyResponseAsync("flow-1", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(outcome.Value.Headers.Get("X-Correlation")).IsEqualTo("abc");
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

        var outcome = await handler.ApplyResponseAsync("flow-2", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(outcome.Value.StatusCode).IsEqualTo(201);
        await Assert.That(outcome.Value.Headers.Get("X-Override")).IsEqualTo("yes");
    }

    /// <summary>
    ///     Verifies that a request-phase script that throws is surfaced as a
    ///     <see cref="ScriptError" /> failure result rather than escaping as a raw exception.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_ScriptThrows_ReturnsScriptErrorFailure()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new ThrowingUserScript("boom-request", throwOnRequest: true));
        var handler = new UserScriptingHandler(configuration);
        var source = BuildRequest("GET");

        var outcome = await handler.ApplyRequestAsync("flow-throw", source, CancellationToken.None);

        await Assert.That(outcome.IsSuccess).IsFalse();
        await Assert.That(outcome.Error).IsTypeOf<ScriptError>();
        await Assert.That(outcome.Error!.Code).IsEqualTo("SCRIPT_REQUEST_FAILED");
        await Assert.That(outcome.Error.Message).IsEqualTo("boom-request");
    }

    /// <summary>
    ///     Verifies that request-phase failures clear flow shared state so response-phase script
    ///     execution cannot observe partially-written state from the failed request.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_ScriptThrows_ClearsSharedStateBeforeResponse()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "request-failure-clears-state",
            onRequest: (request, state) =>
            {
                state["marker"] = "set-before-throw";
                throw new InvalidOperationException("boom-request");
            },
            onResponse: (request, response, state) =>
            {
                response.Headers.Set("X-State-Present", state.ContainsKey("marker") ? "yes" : "no");
            }));
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");
        var sourceResponse = BuildResponse(200);

        var requestOutcome = await handler.ApplyRequestAsync("flow-throw", sourceRequest, CancellationToken.None);
        var responseOutcome = await handler.ApplyResponseAsync("flow-throw", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(requestOutcome.IsSuccess).IsFalse();
        await Assert.That(responseOutcome.IsSuccess).IsTrue();
        await Assert.That(responseOutcome.Value.Headers.Get("X-State-Present")).IsEqualTo("no");
    }

    /// <summary>
    ///     Verifies that a response-phase script that throws is surfaced as a
    ///     <see cref="ScriptError" /> failure result rather than escaping as a raw exception.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_ScriptThrows_ReturnsScriptErrorFailure()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new ThrowingUserScript("boom-response", throwOnResponse: true));
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");
        var sourceResponse = BuildResponse(200);

        var outcome = await handler.ApplyResponseAsync("flow-throw", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(outcome.IsSuccess).IsFalse();
        await Assert.That(outcome.Error).IsTypeOf<ScriptError>();
        await Assert.That(outcome.Error!.Code).IsEqualTo("SCRIPT_RESPONSE_FAILED");
        await Assert.That(outcome.Error.Message).IsEqualTo("boom-response");
    }

    /// <summary>
    ///     Verifies that a script-phase <see cref="OperationCanceledException" /> is still
    ///     propagated to the caller because cancellation is not a script failure.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_ScriptCancels_PropagatesCancellation()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new ThrowingUserScript("cancel", throwOnRequest: true, exceptionFactory: () => new OperationCanceledException("cancelled")));
        var handler = new UserScriptingHandler(configuration);
        var source = BuildRequest("GET");

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await handler.ApplyRequestAsync("flow-cancel", source, CancellationToken.None);
        });
    }

    private sealed class ThrowingUserScript : IUserScript
    {
        private readonly Func<Exception> _exceptionFactory;
        private readonly bool _throwOnRequest;
        private readonly bool _throwOnResponse;

        public ThrowingUserScript(
            string displayName,
            bool throwOnRequest = false,
            bool throwOnResponse = false,
            Func<Exception>? exceptionFactory = null)
        {
            DisplayName = displayName;
            _throwOnRequest = throwOnRequest;
            _throwOnResponse = throwOnResponse;
            _exceptionFactory = exceptionFactory ?? (() => new InvalidOperationException(displayName));
            IsRequestPhaseEnabled = throwOnRequest;
            IsResponsePhaseEnabled = throwOnResponse;
        }

        public string DisplayName { get; }

        public bool IsRequestPhaseEnabled { get; }

        public bool IsResponsePhaseEnabled { get; }

        public Task OnRequestAsync(ScriptableRequest request, IDictionary<string, object?> sharedState, CancellationToken cancellationToken)
        {
            if (_throwOnRequest)
            {
                throw _exceptionFactory();
            }

            return Task.CompletedTask;
        }

        public Task OnResponseAsync(ScriptableRequest request, ScriptableResponse response, IDictionary<string, object?> sharedState, CancellationToken cancellationToken)
        {
            if (_throwOnResponse)
            {
                throw _exceptionFactory();
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Verifies that when the script sets an invalid request URL the handler throws so the
    ///     scripting wrappers can log the failure rather than silently producing malformed HTTP.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_InvalidScriptUrl_Throws()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "bad-url",
            onRequest: (request, state) => request.Url = "not a url"));
        var handler = new UserScriptingHandler(configuration);
        var source = BuildRequest("GET");

        await Assert.That(async () => await handler.ApplyRequestAsync("flow", source, CancellationToken.None))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that when the script sets an out-of-range status code the handler throws so
    ///     the scripting wrappers can log the failure rather than silently producing malformed
    ///     HTTP.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_InvalidScriptStatusCode_Throws()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript(
            "bad-status",
            onResponse: (req, resp, state) => resp.StatusCode = 99999));
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");
        var sourceResponse = BuildResponse(200);

        await Assert.That(async () => await handler.ApplyResponseAsync("flow", sourceRequest, sourceResponse, CancellationToken.None))
            .Throws<InvalidOperationException>();
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
