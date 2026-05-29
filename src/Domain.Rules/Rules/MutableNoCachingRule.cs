using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     A mutable No Caching rule suitable for runtime toggling from the user interface.
///     When enabled, the rule strips cache-validation headers from every request and adds
///     conservative no-cache headers to every response, regardless of URL pattern.
///     A single instance is intended to be registered with <see cref="IRuleRegistry" /> once
///     and toggled throughout the application lifetime via <see cref="SetEnabled" />.
/// </summary>
public sealed class MutableNoCachingRule : IRequestPhaseRule, IResponsePhaseRule
{
    /// <summary>
    ///     Raised whenever the enabled flag changes.
    /// </summary>
    public event MutableNoCachingRuleChanged? Changed;

    private static readonly IReadOnlyList<string> RequestHeadersToStrip;
    private static readonly IReadOnlyList<string> ResponseHeadersToStrip;
    private volatile bool _isEnabled;

    static MutableNoCachingRule()
    {
        var requestStripList = new[]
        {
            "Cache-Control",
            "If-Match",
            "If-Modified-Since",
            "If-None-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Pragma",
        };
        RequestHeadersToStrip = requestStripList;

        var responseStripList = new[]
        {
            "Age",
            "Cache-Control",
            "ETag",
            "Expires",
            "Last-Modified",
            "Pragma",
        };
        ResponseHeadersToStrip = responseStripList;
    }

    /// <summary>
    ///     Initializes a new <see cref="MutableNoCachingRule" />.
    /// </summary>
    /// <param name="priority">The rule's priority within its rule type; lower values execute earlier.</param>
    /// <param name="isEnabled">Whether the rule starts enabled.</param>
    public MutableNoCachingRule(int priority, bool isEnabled)
    {
        Priority = priority;
        _isEnabled = isEnabled;
    }

    /// <inheritdoc />
    public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        if (!_isEnabled)
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
        if (!_isEnabled)
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
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc />
    public int Priority { get; }

    /// <summary>
    ///     Enables or disables the rule. Raises <see cref="Changed" /> only when the state actually changes.
    /// </summary>
    /// <param name="isEnabled">The new enabled state.</param>
    public void SetEnabled(bool isEnabled)
    {
        if (_isEnabled == isEnabled)
        {
            return;
        }

        _isEnabled = isEnabled;
        Changed?.Invoke();
    }
}
