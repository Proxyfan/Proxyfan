using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Allow List rule. When active, only requests whose URL matches one of the configured
///     patterns are permitted; all others are blocked (HTTP 403). The Block List rule (if also
///     configured) still applies to allowed hosts.
/// </summary>
public sealed class AllowListRule : IRequestPhaseRule
{
    private readonly List<IUrlMatcher> _matchers;

    /// <summary>
    ///     Initializes a new <see cref="AllowListRule" /> from the supplied matching rules.
    /// </summary>
    /// <param name="matchingRules">The collection of matching rules that define allowed URLs.</param>
    /// <param name="isEnabled">Whether the rule is active.</param>
    /// <param name="priority">The rule's priority within request-phase rules.</param>
    public AllowListRule(IEnumerable<MatchingRule> matchingRules, bool isEnabled, int priority)
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
        if (_matchers.Count == 0)
        {
            return null;
        }

        var url = request.RequestUri.ToString();

        foreach (var matcher in _matchers)
        {
            if (matcher.HasMatch(url))
            {
                return null;
            }
        }

        return new RequestPipelineAction.Block();
    }

    /// <inheritdoc />
    public bool IsEnabled { get; }

    /// <inheritdoc />
    public int Priority { get; }
}
