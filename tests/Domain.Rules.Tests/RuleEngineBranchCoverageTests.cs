using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Additional branch-coverage tests for <see cref="RuleEngine" /> covering all action types
///     and disabled-rule paths in both request and response phases.
/// </summary>
public sealed class RuleEngineBranchCoverageTests
{
    /// <summary>
    ///     Verifies that ModifyRequest action passes the modified request to later rules but
    ///     does not short-circuit.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_ModifyRequestNotShortCircuit_ContinuesEvaluation()
    {
        var modifiedRequest = CreateRequest("https://example.com/modified");
        var modifyRule = new ScriptedRequestRule(0, new RequestPipelineAction.ModifyRequest(modifiedRequest));
        var blockRule = new ScriptedRequestRule(1, new RequestPipelineAction.Block());
        var engine = new RuleEngine(new IRequestPhaseRule[] { modifyRule, blockRule }, []);

        var actions = engine.EvaluateRequest(CreateRequest("https://example.com/"));

        await Assert.That(actions.Count).IsEqualTo(2);
        await Assert.That(actions[0]).IsTypeOf<RequestPipelineAction.ModifyRequest>();
        await Assert.That(actions[1]).IsTypeOf<RequestPipelineAction.Block>();
    }

    /// <summary>
    ///     Verifies that the response engine skips disabled rules.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_DisabledMixedWithEnabled_OnlyEnabledRulesRun()
    {
        var response = CreateResponse(200);
        var modifiedResponse = CreateResponse(201);
        var disabledRule = new ScriptedResponseRule(0, new ResponsePipelineAction.ModifyResponse(modifiedResponse), isEnabled: false);
        var enabledRule = new ScriptedResponseRule(1, new ResponsePipelineAction.ModifyResponse(modifiedResponse), isEnabled: true);
        var engine = new RuleEngine([], new IResponsePhaseRule[] { disabledRule, enabledRule });
        var request = CreateRequest("https://example.com/");

        var actions = engine.EvaluateResponse(request, response);

        await Assert.That(actions.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a response rule returning null is skipped (no action added).
    /// </summary>
    [Test]
    public async Task EvaluateResponse_RuleReturnsNull_SkippedFromActions()
    {
        var modifiedResponse = CreateResponse(201);
        var nullRule = new ScriptedResponseRule(0, action: null);
        var modifyRule = new ScriptedResponseRule(1, new ResponsePipelineAction.ModifyResponse(modifiedResponse));
        var engine = new RuleEngine([], new IResponsePhaseRule[] { nullRule, modifyRule });
        var request = CreateRequest("https://example.com/");
        var response = CreateResponse(200);

        var actions = engine.EvaluateResponse(request, response);

        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(actions[0]).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData CreateResponse(int statusCode)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

    private sealed class ScriptedRequestRule : IRequestPhaseRule
    {
        private readonly RequestPipelineAction? _action;

        public ScriptedRequestRule(int priority, RequestPipelineAction? action)
        {
            Priority = priority;
            _action = action;
            IsEnabled = true;
        }

        public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
        {
            return _action;
        }

        public bool IsEnabled { get; }

        public int Priority { get; }
    }

    private sealed class ScriptedResponseRule : IResponsePhaseRule
    {
        private readonly ResponsePipelineAction? _action;

        public ScriptedResponseRule(int priority, ResponsePipelineAction? action, bool isEnabled = true)
        {
            Priority = priority;
            _action = action;
            IsEnabled = isEnabled;
        }

        public ResponsePipelineAction? EvaluateResponse(
            HypertextTransferProtocolRequestData request,
            HypertextTransferProtocolResponseData response)
        {
            return _action;
        }

        public bool IsEnabled { get; }

        public int Priority { get; }
    }
}
