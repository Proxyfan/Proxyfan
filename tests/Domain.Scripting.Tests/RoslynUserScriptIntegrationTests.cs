using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     End-to-end tests that exercise <see cref="RoslynUserScriptCompiler" /> through
///     <see cref="UserScriptingHandler" /> to confirm compiled user code can mutate
///     requests and responses, and share state across phases for the same flow.
/// </summary>
public sealed class RoslynUserScriptIntegrationTests
{
    /// <summary>
    ///     Verifies that a compiled response-phase script can read shared state set by the
    ///     request-phase script on the same flow.
    /// </summary>
    [Test]
    public async Task EndToEnd_SharedStateAcrossPhases_RoundTrips()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string requestSource = "SharedState[\"flow-tag\"] = Request.Method;";
        const string responseSource = "Response.Headers.Set(\"X-Echoed-Method\", (string)(SharedState[\"flow-tag\"] ?? \"missing\"));";
        var compilation = compiler.Compile("shared", requestSource, responseSource);
        await Assert.That(compilation.IsSuccess).IsTrue();
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(compilation.Script!);
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("POST");
        var sourceResponse = BuildResponse(200);

        await handler.ApplyRequestAsync("flow-end-to-end", sourceRequest, CancellationToken.None);
        var result = await handler.ApplyResponseAsync("flow-end-to-end", sourceRequest, sourceResponse, CancellationToken.None);

        await Assert.That(result.Headers.Get("X-Echoed-Method")).IsEqualTo("POST");
    }

    /// <summary>
    ///     Verifies that a compiled request-phase script can mutate the request method and
    ///     add a header via the <see cref="ScriptableRequest" /> view.
    /// </summary>
    [Test]
    public async Task EndToEnd_UserScriptMutatesRequest_AppliesChanges()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string source = "Request.Method = \"PATCH\"; Request.Headers.Set(\"X-By-Script\", \"yes\");";
        var compilation = compiler.Compile("mutator", source, string.Empty);
        await Assert.That(compilation.IsSuccess).IsTrue();
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(compilation.Script!);
        var handler = new UserScriptingHandler(configuration);
        var sourceRequest = BuildRequest("GET");

        var result = await handler.ApplyRequestAsync("flow", sourceRequest, CancellationToken.None);

        await Assert.That(result.Method).IsEqualTo("PATCH");
        await Assert.That(result.Headers.Get("X-By-Script")).IsEqualTo("yes");
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
