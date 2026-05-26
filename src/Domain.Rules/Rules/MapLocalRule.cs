using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Map Local rule. Matches a URL pattern and returns a locally-configured response
///     (inline body, fixed status, fixed headers) instead of forwarding to the upstream server.
/// </summary>
public sealed class MapLocalRule : IRequestPhaseRule
{
    private readonly ReadOnlyMemory<byte> _body;
    private readonly HeaderCollection _headers;
    private readonly IUrlMatcher _matcher;
    private readonly string _reasonPhrase;
    private readonly int _statusCode;

    /// <summary>
    ///     Initializes a new <see cref="MapLocalRule" /> with an inline response body.
    /// </summary>
    /// <param name="matchingRule">The pattern used to match requests.</param>
    /// <param name="parameters">The configuration parameters for the local response.</param>
    public MapLocalRule(MatchingRule matchingRule, MapLocalRuleParameters parameters)
    {
        if (parameters.StatusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.StatusCode, "Status code must be between 100 and 599.");
        }

        _matcher = matchingRule.Compile();
        _statusCode = parameters.StatusCode;
        _reasonPhrase = parameters.ReasonPhrase;
        _body = parameters.Body;

        var headers = HeaderCollection.Empty;
        foreach (var header in parameters.Headers)
        {
            headers = headers.Add(header.Key, header.Value);
        }

        _headers = headers;
        IsEnabled = parameters.IsEnabled;
        Priority = parameters.Priority;
    }

    /// <inheritdoc />
    public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var url = request.RequestUri.ToString();

        if (!_matcher.HasMatch(url))
        {
            return null;
        }

        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = _body,
            Headers = _headers,
            ReasonPhrase = _reasonPhrase,
            StatusCode = _statusCode,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);
        return new RequestPipelineAction.ServeLocalResponse(response);
    }

    /// <inheritdoc />
    public bool IsEnabled { get; }

    /// <inheritdoc />
    public int Priority { get; }
}
