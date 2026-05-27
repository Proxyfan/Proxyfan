using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model representing a single pending breakpoint pause. Surfaces editable
///     fields for the request or response and produces a fresh
///     <see cref="HypertextTransferProtocolRequestData" /> or
///     <see cref="HypertextTransferProtocolResponseData" /> instance when the user
///     decides to resume.
/// </summary>
public sealed class BreakpointPauseViewModel
{
    /// <summary>
    ///     Gets or sets the editable request body as a UTF-8 string.
    /// </summary>
    public string BodyText { get; set; }

    /// <summary>
    ///     Gets or sets the editable headers as a multi-line string (one <c>Name: Value</c> per line).
    /// </summary>
    public string HeadersText { get; set; }

    /// <summary>
    ///     Gets or sets the editable HTTP method for request-phase pauses. Empty for response-phase.
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    ///     Gets the underlying domain pause object.
    /// </summary>
    public BreakpointPause Pause { get; }

    /// <summary>
    ///     Gets the breakpoint phase represented by this pause.
    /// </summary>
    public BreakpointPhase Phase { get; }

    /// <summary>
    ///     Gets or sets the editable response reason phrase for response-phase pauses. Empty for request-phase.
    /// </summary>
    public string ReasonPhrase { get; set; }

    /// <summary>
    ///     Gets or sets the editable request URL.
    /// </summary>
    public string RequestUrl { get; set; }

    /// <summary>
    ///     Gets or sets the editable response status code for response-phase pauses. Zero for request-phase.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="BreakpointPauseViewModel" /> populated from the supplied pause.
    /// </summary>
    /// <param name="pause">The pause to wrap.</param>
    public BreakpointPauseViewModel(BreakpointPause pause)
    {
        Pause = pause;
        Phase = pause.Phase;
        RequestUrl = pause.Request.RequestUri.AbsoluteUri;
        Method = pause.Request.Method;

        if (pause.Phase == BreakpointPhase.Request)
        {
            HeadersText = BreakpointMessageTextHelpers.FormatHeaders(pause.Request.Headers);
            BodyText = BreakpointMessageTextHelpers.DecodeBody(pause.Request.Body);
            StatusCode = 0;
            ReasonPhrase = string.Empty;
        }
        else
        {
            var response = pause.Response!;
            HeadersText = BreakpointMessageTextHelpers.FormatHeaders(response.Headers);
            BodyText = BreakpointMessageTextHelpers.DecodeBody(response.Body);
            StatusCode = response.StatusCode;
            ReasonPhrase = response.ReasonPhrase;
        }
    }

    /// <summary>
    ///     Builds a request-phase decision from the current editor state.
    /// </summary>
    /// <returns>A decision that resumes the pause with the edited request.</returns>
    public BreakpointDecision BuildRequestDecision()
    {
        var requestUri = new Uri(RequestUrl);
        var body = BreakpointMessageTextHelpers.EncodeBody(BodyText);
        var headers = BreakpointMessageTextHelpers.ParseHeaders(HeadersText);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body,
            Headers = headers,
            Method = Method,
            RequestUri = requestUri,
            Version = Pause.Request.Version,
        };
        var data = new HypertextTransferProtocolRequestData(parameters);
        return BreakpointDecisions.ResumeRequest(data);
    }

    /// <summary>
    ///     Builds a response-phase decision from the current editor state.
    /// </summary>
    /// <returns>A decision that resumes the pause with the edited response.</returns>
    public BreakpointDecision BuildResponseDecision()
    {
        var body = BreakpointMessageTextHelpers.EncodeBody(BodyText);
        var headers = BreakpointMessageTextHelpers.ParseHeaders(HeadersText);
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = ReasonPhrase,
            StatusCode = StatusCode,
            Version = Pause.Response!.Version,
        };
        var data = new HypertextTransferProtocolResponseData(parameters);
        return BreakpointDecisions.ResumeResponse(data);
    }
}
