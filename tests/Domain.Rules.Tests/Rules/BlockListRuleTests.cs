using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="BlockListRule" />.
/// </summary>
public sealed class BlockListRuleTests
{
    /// <summary>
    ///     Verifies that the rule blocks requests matching any pattern.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingPattern_ReturnsBlock()
    {
        var rules = new[]
        {
            new MatchingRule("https://blocked.example/*", MatchingRuleKind.Wildcard),
        };
        var blockList = new BlockListRule(rules, isEnabled: true, priority: 0);
        var request = CreateRequest("https://blocked.example/path");

        var action = blockList.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.Block>();
    }

    /// <summary>
    ///     Verifies that the rule does not block non-matching requests.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingPattern_ReturnsNull()
    {
        var rules = new[]
        {
            new MatchingRule("https://blocked.example/*", MatchingRuleKind.Wildcard),
        };
        var blockList = new BlockListRule(rules, isEnabled: true, priority: 0);
        var request = CreateRequest("https://allowed.example/");

        var action = blockList.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Verifies that an empty block list never blocks anything.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_EmptyPatternList_ReturnsNull()
    {
        var blockList = new BlockListRule(Enumerable.Empty<MatchingRule>(), isEnabled: true, priority: 0);
        var request = CreateRequest("https://anywhere.example/");

        var action = blockList.EvaluateRequest(request);

        await Assert.That(action).IsNull();
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
}
