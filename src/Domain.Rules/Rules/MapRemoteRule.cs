using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Map Remote rule. Rewrites the destination URL (scheme/host/port/path) of matching requests
///     before they leave the proxy. Empty destination fields preserve the original component.
/// </summary>
public sealed class MapRemoteRule : IRequestPhaseRule
{
    private readonly MapRemoteDestination _destination;
    private readonly IUrlMatcher _matcher;

    /// <summary>
    ///     Initializes a new <see cref="MapRemoteRule" />.
    /// </summary>
    /// <param name="matchingRule">The pattern used to select requests for rewriting.</param>
    /// <param name="destination">The destination components.</param>
    /// <param name="isEnabled">Whether the rule is active.</param>
    /// <param name="priority">The rule's priority within request-phase rules.</param>
    public MapRemoteRule(
        MatchingRule matchingRule,
        MapRemoteDestination destination,
        bool isEnabled,
        int priority)
    {
        _matcher = matchingRule.Compile();
        _destination = destination;
        IsEnabled = isEnabled;
        Priority = priority;
    }

    /// <inheritdoc />
    public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var originalUrl = request.RequestUri.ToString();

        if (!_matcher.HasMatch(originalUrl))
        {
            return null;
        }

        var rewrittenUri = MapRemoteUriRewriter.Rewrite(request.RequestUri, _destination);
        var rewrittenHeaders = _destination.IsPreservingHostHeader
            ? request.Headers
            : MapRemoteHeaderRewriter.ReplaceHostHeader(request.Headers, rewrittenUri);

        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = request.Body,
            Headers = rewrittenHeaders,
            Method = request.Method,
            RequestUri = rewrittenUri,
            Version = request.Version,
        };
        var rewrittenRequest = new HypertextTransferProtocolRequestData(parameters);
        return new RequestPipelineAction.Redirect(rewrittenRequest);
    }

    /// <inheritdoc />
    public bool IsEnabled { get; }

    /// <inheritdoc />
    public int Priority { get; }
}
