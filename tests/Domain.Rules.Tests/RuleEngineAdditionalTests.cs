using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Additional tests for <see cref="RuleEngine" /> covering remaining branches.
/// </summary>
public sealed class RuleEngineAdditionalTests
{
    /// <summary>
    ///     Verifies that <see cref="RequestPipelineAction.ModifyRequest" /> updates the request
    ///     seen by later rules.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_ModifyRequestAction_PassesModifiedRequestToLaterRules()
    {
        var modifiedRequest = CreateRequest("https://example.com/modified");
        var modifyRule = new ScriptedRequestRule(priority: 0, action: new RequestPipelineAction.ModifyRequest(modifiedRequest));
        var inspector = new InspectingRequestRule(priority: 1);
        var engine = new RuleEngine(new IRequestPhaseRule[] { modifyRule, inspector }, Enumerable.Empty<IResponsePhaseRule>());
        var originalRequest = CreateRequest("https://example.com/original");

        engine.EvaluateRequest(originalRequest);

        await Assert.That(inspector.SeenUrls.Count).IsEqualTo(1);
        await Assert.That(inspector.SeenUrls[0]).IsEqualTo("https://example.com/modified");
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

    private sealed class InspectingRequestRule : IRequestPhaseRule
    {
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

        public bool IsEnabled { get; }

        public int Priority { get; }

        public List<string> SeenUrls { get; }
    }
}
