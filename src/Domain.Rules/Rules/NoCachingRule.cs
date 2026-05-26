using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     No Caching rule. Strips conditional-request and cache-validation headers from matching
///     requests and matching responses to force fresh content on every roundtrip.
/// </summary>
public sealed class NoCachingRule : IRequestPhaseRule, IResponsePhaseRule
{
    private static readonly IReadOnlyList<string> RequestHeadersToStrip;
    private static readonly IReadOnlyList<string> ResponseHeadersToStrip;
    private readonly IUrlMatcher _matcher;

    static NoCachingRule()
    {
        var requestStripList = new[]
        {
            "Cache-Control",
            "If-Modified-Since",
            "If-None-Match",
            "If-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Pragma",
        };
        RequestHeadersToStrip = requestStripList;

        var responseStripList = new[]
        {
            "Cache-Control",
            "ETag",
            "Last-Modified",
            "Expires",
            "Age",
            "Pragma",
        };
        ResponseHeadersToStrip = responseStripList;
    }

    /// <summary>
    ///     Initializes a new <see cref="NoCachingRule" />.
    /// </summary>
    /// <param name="matchingRule">The pattern used to match requests and responses.</param>
    /// <param name="isEnabled">Whether the rule is active.</param>
    /// <param name="priority">The rule's priority within its rule type.</param>
    public NoCachingRule(MatchingRule matchingRule, bool isEnabled, int priority)
    {
        _matcher = matchingRule.Compile();
        IsEnabled = isEnabled;
        Priority = priority;
    }

    /// <inheritdoc />
    public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var url = request.RequestUri.ToString();

        if (!_matcher.HasMatch(url))
        {
            return null;
        }

        var strippedHeaders = HeaderStripper.StripHeaders(request.Headers, RequestHeadersToStrip);
        strippedHeaders = strippedHeaders.Add("Cache-Control", "no-cache");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = request.Body,
            Headers = strippedHeaders,
            Method = request.Method,
            RequestUri = request.RequestUri,
            Version = request.Version,
        };
        var modifiedRequest = new HypertextTransferProtocolRequestData(parameters);
        return new RequestPipelineAction.ModifyRequest(modifiedRequest);
    }

    /// <inheritdoc />
    public ResponsePipelineAction? EvaluateResponse(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        var url = request.RequestUri.ToString();

        if (!_matcher.HasMatch(url))
        {
            return null;
        }

        var strippedHeaders = HeaderStripper.StripHeaders(response.Headers, ResponseHeadersToStrip);
        strippedHeaders = strippedHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = response.Body,
            Headers = strippedHeaders,
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            Version = response.Version,
        };
        var modifiedResponse = new HypertextTransferProtocolResponseData(parameters);
        return new ResponsePipelineAction.ModifyResponse(modifiedResponse);
    }

    /// <inheritdoc />
    public bool IsEnabled { get; }

    /// <inheritdoc />
    public int Priority { get; }
}
