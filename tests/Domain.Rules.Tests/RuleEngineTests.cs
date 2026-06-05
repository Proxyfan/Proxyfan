using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="RuleEngine" />.
/// </summary>
public sealed class RuleEngineTests
{
    /// <summary>
    ///     Verifies that with no rules, the engine returns no actions.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NoRules_ReturnsEmpty()
    {
        var engine = new RuleEngine(Enumerable.Empty<IRequestPhaseRule>(), Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://example.com/");

        var actions = engine.EvaluateRequest(request);

        await Assert.That(actions.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that rules execute in ascending priority order.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MultipleRules_ExecutedInPriorityOrder()
    {
        var executed = new List<string>();
        var ruleHighPriority = new RecordingRequestRule("low", priority: 5, action: null, executed: executed);
        var ruleLowPriority = new RecordingRequestRule("first", priority: 1, action: null, executed: executed);
        var engine = new RuleEngine(new IRequestPhaseRule[] { ruleHighPriority, ruleLowPriority }, Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://example.com/");

        engine.EvaluateRequest(request);

        await Assert.That(executed.Count).IsEqualTo(2);
        await Assert.That(executed[0]).IsEqualTo("first");
        await Assert.That(executed[1]).IsEqualTo("low");
    }

    /// <summary>
    ///     Verifies that a <see cref="RequestPipelineAction.Block" /> short-circuits later rules.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_BlockAction_ShortCircuitsLaterRules()
    {
        var executed = new List<string>();
        var blockRule = new RecordingRequestRule("block", priority: 0, action: new RequestPipelineAction.Block(), executed: executed);
        var laterRule = new RecordingRequestRule("never", priority: 1, action: null, executed: executed);
        var engine = new RuleEngine(new IRequestPhaseRule[] { blockRule, laterRule }, Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://example.com/");

        var actions = engine.EvaluateRequest(request);

        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(actions[0]).IsTypeOf<RequestPipelineAction.Block>();
        await Assert.That(executed.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <see cref="RequestPipelineAction.ServeLocalResponse" /> short-circuits later rules.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_ServeLocalResponse_ShortCircuitsLaterRules()
    {
        var executed = new List<string>();
        var serveResponse = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        var serveRule = new RecordingRequestRule("serve", priority: 0, action: new RequestPipelineAction.ServeLocalResponse(serveResponse), executed: executed);
        var laterRule = new RecordingRequestRule("never", priority: 1, action: null, executed: executed);
        var engine = new RuleEngine(new IRequestPhaseRule[] { serveRule, laterRule }, Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://example.com/");

        var actions = engine.EvaluateRequest(request);

        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(executed.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a <see cref="RequestPipelineAction.Redirect" /> updates the request seen by later rules.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_Redirect_PassesRewrittenRequestToLaterRules()
    {
        var rewritten = CreateRequest("https://rewritten.example/");
        var redirectRule = new RecordingRequestRule("redirect", priority: 0, action: new RequestPipelineAction.Redirect(rewritten), executed: new List<string>());
        var sawRewrittenRule = new InspectingRequestRule(priority: 1);
        var engine = new RuleEngine(new IRequestPhaseRule[] { redirectRule, sawRewrittenRule }, Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://original.example/");

        engine.EvaluateRequest(request);

        await Assert.That(sawRewrittenRule.SeenUrls.Count).IsEqualTo(1);
        await Assert.That(sawRewrittenRule.SeenUrls[0]).IsEqualTo("https://rewritten.example/");
    }

    /// <summary>
    ///     Verifies that disabled rules are skipped entirely.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_DisabledRule_IsSkipped()
    {
        var executed = new List<string>();
        var disabledRule = new RecordingRequestRule("disabled", priority: 0, action: new RequestPipelineAction.Block(), executed: executed, isEnabled: false);
        var engine = new RuleEngine(new IRequestPhaseRule[] { disabledRule }, Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://example.com/");

        var actions = engine.EvaluateRequest(request);

        await Assert.That(actions.Count).IsEqualTo(0);
        await Assert.That(executed.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that with no rules, EvaluateResponse returns no actions.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_NoRules_ReturnsEmpty()
    {
        var engine = new RuleEngine(Enumerable.Empty<IRequestPhaseRule>(), Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://example.com/");
        var response = CreateResponse(200);

        var actions = engine.EvaluateResponse(request, response);

        await Assert.That(actions.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that response-phase modify actions chain through.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_TwoModifyRules_ChainsModifiedResponses()
    {
        var firstModified = CreateResponse(201);
        var secondModified = CreateResponse(202);
        var first = new RecordingResponseRule(priority: 0, action: new ResponsePipelineAction.ModifyResponse(firstModified));
        var second = new RecordingResponseRule(priority: 1, action: new ResponsePipelineAction.ModifyResponse(secondModified));
        var engine = new RuleEngine(Enumerable.Empty<IRequestPhaseRule>(), new IResponsePhaseRule[] { first, second });
        var request = CreateRequest("https://example.com/");
        var initialResponse = CreateResponse(200);

        var actions = engine.EvaluateResponse(request, initialResponse);

        await Assert.That(actions.Count).IsEqualTo(2);
        // Second rule should have seen the first rule's output (201)
        await Assert.That(second.LastSeenResponseStatus).IsEqualTo(201);
    }

    /// <summary>
    ///     Verifies that disabled response rules are skipped.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_DisabledRule_IsSkipped()
    {
        var modified = CreateResponse(201);
        var rule = new RecordingResponseRule(priority: 0, action: new ResponsePipelineAction.ModifyResponse(modified), isEnabled: false);
        var engine = new RuleEngine(Enumerable.Empty<IRequestPhaseRule>(), new IResponsePhaseRule[] { rule });
        var request = CreateRequest("https://example.com/");
        var response = CreateResponse(200);

        var actions = engine.EvaluateResponse(request, response);

        await Assert.That(actions.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a throwing sync request-phase rule is caught, skipped, and later rules
    ///     still execute so the engine never propagates the exception.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_ThrowingRule_IsSkippedAndLaterRulesStillRun()
    {
        var executed = new List<string>();
        var throwingRule = new ThrowingRequestRule("throwing", priority: 0, executed: executed);
        var laterRule = new RecordingRequestRule("later", priority: 1, action: null, executed: executed);
        var engine = new RuleEngine(new IRequestPhaseRule[] { throwingRule, laterRule }, Enumerable.Empty<IResponsePhaseRule>());
        var request = CreateRequest("https://example.com/");

        var actions = engine.EvaluateRequest(request);

        await Assert.That(actions.Count).IsEqualTo(0);
        await Assert.That(executed).Contains("throwing");
        await Assert.That(executed).Contains("later");
    }

    /// <summary>
    ///     Verifies that a throwing sync response-phase rule is caught, skipped, and later rules
    ///     still execute so the engine never propagates the exception.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_ThrowingRule_IsSkippedAndLaterRulesStillRun()
    {
        var executed = new List<string>();
        var throwingRule = new ThrowingResponseRule("throwing", priority: 0, executed: executed);
        var laterModified = CreateResponse(201);
        var laterRule = new RecordingResponseRule(priority: 1, action: new ResponsePipelineAction.ModifyResponse(laterModified));
        var engine = new RuleEngine(Enumerable.Empty<IRequestPhaseRule>(), new IResponsePhaseRule[] { throwingRule, laterRule });
        var request = CreateRequest("https://example.com/");
        var response = CreateResponse(200);

        var actions = engine.EvaluateResponse(request, response);

        await Assert.That(executed).Contains("throwing");
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

    private sealed class RecordingRequestRule : IRequestPhaseRule
    {
        private readonly RequestPipelineAction? _action;
        private readonly List<string> _executed;
        private readonly string _name;

        public bool IsEnabled { get; }

        public int Priority { get; }

        public RecordingRequestRule(string name, int priority, RequestPipelineAction? action, List<string> executed, bool isEnabled = true)
        {
            _name = name;
            Priority = priority;
            _action = action;
            _executed = executed;
            IsEnabled = isEnabled;
        }

        public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
        {
            _executed.Add(_name);
            return _action;
        }
    }

    private sealed class InspectingRequestRule : IRequestPhaseRule
    {
        public bool IsEnabled { get; }

        public int Priority { get; }

        public List<string> SeenUrls { get; }

        public InspectingRequestRule(int priority)
        {
            Priority = priority;
            IsEnabled = true;
            var seenUrls = new List<string>();
            SeenUrls = seenUrls;
        }

        public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
        {
            SeenUrls.Add(request.RequestUri.ToString());
            return null;
        }
    }

    private sealed class RecordingResponseRule : IResponsePhaseRule
    {
        private readonly ResponsePipelineAction? _action;

        public bool IsEnabled { get; }

        public int LastSeenResponseStatus { get; private set; }

        public int Priority { get; }

        public RecordingResponseRule(int priority, ResponsePipelineAction? action, bool isEnabled = true)
        {
            Priority = priority;
            _action = action;
            IsEnabled = isEnabled;
        }

        public ResponsePipelineAction? EvaluateResponse(
            HypertextTransferProtocolRequestData request,
            HypertextTransferProtocolResponseData response)
        {
            LastSeenResponseStatus = response.StatusCode;
            return _action;
        }
    }

    private sealed class ThrowingRequestRule : IRequestPhaseRule
    {
        private readonly List<string> _executed;
        private readonly string _name;

        public bool IsEnabled => true;

        public int Priority { get; }

        public ThrowingRequestRule(string name, int priority, List<string> executed)
        {
            _name = name;
            Priority = priority;
            _executed = executed;
        }

        public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
        {
            _executed.Add(_name);
            throw new InvalidOperationException("Simulated rule failure");
        }
    }

    private sealed class ThrowingResponseRule : IResponsePhaseRule
    {
        private readonly List<string> _executed;
        private readonly string _name;

        public bool IsEnabled => true;

        public int Priority { get; }

        public ThrowingResponseRule(string name, int priority, List<string> executed)
        {
            _name = name;
            Priority = priority;
            _executed = executed;
        }

        public ResponsePipelineAction? EvaluateResponse(
            HypertextTransferProtocolRequestData request,
            HypertextTransferProtocolResponseData response)
        {
            _executed.Add(_name);
            throw new InvalidOperationException("Simulated rule failure");
        }
    }
}
