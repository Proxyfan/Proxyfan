using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Helpers that materialize immutable
///     <see cref="HypertextTransferProtocolRequestData" /> / <see cref="HypertextTransferProtocolResponseData" />
///     instances from the mutable script views. The script-editable surface (URL, method,
///     status code, reason phrase, header name and value) is validated against the HTTP/1.1
///     wire grammar before construction so that a script typo surfaces as a typed
///     <see cref="ScriptError" /> rather than throwing from <see cref="Uri" />'s constructor
///     or emitting malformed HTTP downstream.
/// </summary>
public static class ScriptableProjector
{
    /// <summary>
    ///     Materializes an immutable request from the supplied script view, copying body bytes
    ///     from the supplied source request unchanged. The request URL, method, and every
    ///     header name/value are validated; on failure a <see cref="ScriptError" /> is returned
    ///     and the caller is expected to leave the original request unmodified.
    /// </summary>
    /// <param name="view">The mutated script view.</param>
    /// <param name="source">The source request whose body and version are reused.</param>
    /// <returns>A success result with the projected request, or a failure result describing the validation error.</returns>
    public static Result<HypertextTransferProtocolRequestData> Project(
        ScriptableRequest view,
        HypertextTransferProtocolRequestData source)
    {
        if (!ScriptableProjectorValidator.HasValidUrl(view.Url))
        {
            var error = new ScriptError(
                "SCRIPT_INVALID_REQUEST_URL",
                $"Script set an invalid request URL: '{view.Url}'. Expected an absolute URI (e.g. https://host/path).");
            return Result.Failure<HypertextTransferProtocolRequestData>(error);
        }

        if (!ScriptableProjectorValidator.HasValidMethod(view.Method))
        {
            var error = new ScriptError(
                "SCRIPT_INVALID_REQUEST_METHOD",
                $"Script set an invalid request method: '{view.Method}'. Expected a non-empty HTTP token (RFC 7230 §3.1.1).");
            return Result.Failure<HypertextTransferProtocolRequestData>(error);
        }

        var headersResult = BuildHeaders(view.Headers);
        if (!headersResult.IsSuccess)
        {
            return Result.Failure<HypertextTransferProtocolRequestData>(headersResult.Error!);
        }

        var requestUri = new Uri(view.Url);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = source.Body,
            Headers = headersResult.Value,
            Method = view.Method,
            RequestUri = requestUri,
            Version = source.Version,
        };
        var data = new HypertextTransferProtocolRequestData(parameters);
        return Result.Success(data);
    }

    /// <summary>
    ///     Materializes an immutable response from the supplied script view, copying body bytes
    ///     and version from the supplied source response unchanged. The status code, reason
    ///     phrase, and every header name/value are validated; on failure a
    ///     <see cref="ScriptError" /> is returned and the caller is expected to leave the
    ///     original response unmodified.
    /// </summary>
    /// <param name="view">The mutated script view.</param>
    /// <param name="source">The source response whose body is reused.</param>
    /// <returns>A success result with the projected response, or a failure result describing the validation error.</returns>
    public static Result<HypertextTransferProtocolResponseData> Project(
        ScriptableResponse view,
        HypertextTransferProtocolResponseData source)
    {
        if (!ScriptableProjectorValidator.HasValidStatusCode(view.StatusCode))
        {
            var error = new ScriptError(
                "SCRIPT_INVALID_RESPONSE_STATUS_CODE",
                $"Script set an invalid response status code: {view.StatusCode}. Expected a 3-digit value in 100–999 (RFC 7230 §3.1.2).");
            return Result.Failure<HypertextTransferProtocolResponseData>(error);
        }

        if (!ScriptableProjectorValidator.HasValidReasonPhrase(view.ReasonPhrase))
        {
            var error = new ScriptError(
                "SCRIPT_INVALID_RESPONSE_REASON_PHRASE",
                $"Script set an invalid response reason phrase: '{view.ReasonPhrase}'. Control characters (CR, LF, NUL) are forbidden (RFC 7230 §3.1.2).");
            return Result.Failure<HypertextTransferProtocolResponseData>(error);
        }

        var headersResult = BuildHeaders(view.Headers);
        if (!headersResult.IsSuccess)
        {
            return Result.Failure<HypertextTransferProtocolResponseData>(headersResult.Error!);
        }

        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = source.Body,
            Headers = headersResult.Value,
            ReasonPhrase = view.ReasonPhrase,
            StatusCode = view.StatusCode,
            Version = source.Version,
        };
        var data = new HypertextTransferProtocolResponseData(parameters);
        return Result.Success(data);
    }

    private static Result<HeaderCollection> BuildHeaders(ScriptableHeaders source)
    {
        var headers = HeaderCollection.Empty;
        foreach (var header in source.Enumerate())
        {
            if (!ScriptableProjectorValidator.HasValidHeaderName(header.Key))
            {
                var error = new ScriptError(
                    "SCRIPT_INVALID_HEADER_NAME",
                    $"Script set an invalid header name: '{header.Key}'. Expected a non-empty HTTP token (RFC 7230 §3.2.6).");
                return Result.Failure<HeaderCollection>(error);
            }

            if (!ScriptableProjectorValidator.HasValidHeaderValue(header.Value))
            {
                var error = new ScriptError(
                    "SCRIPT_INVALID_HEADER_VALUE",
                    $"Script set an invalid value for header '{header.Key}'. Control characters (CR, LF, NUL) are forbidden (RFC 7230 §3.2.4).");
                return Result.Failure<HeaderCollection>(error);
            }

            headers = headers.Add(header.Key, header.Value);
        }

        return Result.Success(headers);
    }
}
