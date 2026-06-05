using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Block List rule. Any request whose URL matches one of the configured patterns is rejected
///     (HTTP 403) and remaining request-phase rules are skipped.
/// </summary>
public sealed class BlockListRule : IRequestPhaseRule
{
    private readonly List<IUrlMatcher> _matchers;

    /// <summary>
    ///     Initializes a new <see cref="BlockListRule" /> from the supplied matching rules.
    /// </summary>
    /// <param name="matchingRules">The collection of matching rules that define blocked URLs.</param>
    /// <param name="isEnabled">Whether the rule is active.</param>
    /// <param name="priority">The rule's priority within request-phase rules.</param>
    public BlockListRule(IEnumerable<MatchingRule> matchingRules, bool isEnabled, int priority)
    {
        var matchers = new List<IUrlMatcher>();
        foreach (var rule in matchingRules)
        {
            var matcher = rule.Compile();
            matchers.Add(matcher);
        }

        _matchers = matchers;
        IsEnabled = isEnabled;
        Priority = priority;
    }

    /// <inheritdoc />
    public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var url = request.RequestUri.ToString();

        foreach (var matcher in _matchers)
        {
            var matchResult = matcher.GetMatchResult(url);
            if (matchResult is UrlMatchResult.Match or UrlMatchResult.Indeterminate)
            {
                return new RequestPipelineAction.Block();
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled { get; }

    /// <inheritdoc />
    public int Priority { get; }
}
