using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Domain.Session.Har;

/// <summary>
///     Static helpers that parse individual HAR entries into <see cref="TrafficFlow" /> instances.
/// </summary>
public static class HarEntryParser
{
    /// <summary>
    ///     Parses a single HAR entry element into a <see cref="TrafficFlow" />. Returns null when
    ///     the entry is missing required fields.
    /// </summary>
    /// <param name="entry">The HAR entry JSON element.</param>
    /// <returns>The reconstructed traffic flow, or null when the entry is invalid.</returns>
    public static TrafficFlow? ParseEntry(JsonElement entry)
    {
        return ParseEntry(entry, int.MaxValue);
    }

    /// <summary>
    ///     Parses a single HAR entry element into a <see cref="TrafficFlow" /> with a body-size limit.
    /// </summary>
    /// <param name="entry">The HAR entry JSON element.</param>
    /// <param name="maxEntryBodyBytes">Maximum request/response body bytes retained per entry.</param>
    /// <returns>The reconstructed traffic flow, or null when the entry is invalid.</returns>
    public static TrafficFlow? ParseEntry(JsonElement entry, int maxEntryBodyBytes)
    {
        var startedAt = ExtractStartedDateTime(entry);
        var clientEndPoint = ExtractClientEndPoint(entry);
        var flowId = ExtractFlowId(entry);
        var flow = new TrafficFlow(flowId, clientEndPoint, startedAt);

        if (entry.TryGetProperty("request", out var requestElement))
        {
            var request = ParseRequest(requestElement, maxEntryBodyBytes, out var hasRequestBodyBeenTruncated);

            if (request is not null)
            {
                flow.SetRequest(request);

                if (hasRequestBodyBeenTruncated)
                {
                    flow.UpdateRequestBodyForStorage(request.Body, null, isTruncated: true);
                }
            }
        }

        if (entry.TryGetProperty("response", out var responseElement))
        {
            var response = ParseResponse(responseElement, maxEntryBodyBytes, out var hasResponseBodyBeenTruncated);

            if (response is not null && flow.Status == TrafficFlowStatus.Active)
            {
                flow.SetResponse(response);
                flow.Complete();

                if (hasResponseBodyBeenTruncated)
                {
                    flow.UpdateResponseBodyForStorage(response.Body, null, isTruncated: true);
                }
            }
        }

        var colorTag = ExtractColorTag(entry);
        if (colorTag != TrafficFlowColorTag.None)
        {
            flow.SetColorTag(colorTag);
        }

        var comment = ExtractComment(entry);
        if (comment is not null)
        {
            flow.SetComment(comment);
        }

        return flow;
    }

    private static string ExtractClientEndPoint(JsonElement entry)
    {
        if (entry.TryGetProperty("_proxyfanClientEndPoint", out var endPointElement)
            && endPointElement.ValueKind == JsonValueKind.String)
        {
            var value = endPointElement.GetString();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "unknown";
    }

    private static TrafficFlowColorTag ExtractColorTag(JsonElement entry)
    {
        if (entry.TryGetProperty("_proxyfanColorTag", out var colorElement)
            && colorElement.ValueKind == JsonValueKind.String
            && Enum.TryParse<TrafficFlowColorTag>(colorElement.GetString(), ignoreCase: true, out var colorTag))
        {
            return colorTag;
        }

        return TrafficFlowColorTag.None;
    }

    private static string? ExtractComment(JsonElement entry)
    {
        if (entry.TryGetProperty("_proxyfanComment", out var commentElement)
            && commentElement.ValueKind == JsonValueKind.String)
        {
            var value = commentElement.GetString();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static Guid ExtractFlowId(JsonElement entry)
    {
        if (entry.TryGetProperty("_proxyfanFlowId", out var flowIdElement)
            && flowIdElement.ValueKind == JsonValueKind.String
            && Guid.TryParse(flowIdElement.GetString(), out var flowId))
        {
            return flowId;
        }

        return Guid.NewGuid();
    }

    private static DateTimeOffset ExtractStartedDateTime(JsonElement entry)
    {
        if (entry.TryGetProperty("startedDateTime", out var startedElement)
            && startedElement.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(startedElement.GetString(), CultureInfo.InvariantCulture, out var startedAt))
        {
            return startedAt;
        }

        return DateTimeOffset.UtcNow;
    }

    private static string ExtractStringOrDefault(JsonElement element, string propertyName, string defaultValue)
    {
        if (element.TryGetProperty(propertyName, out var stringElement)
            && stringElement.ValueKind == JsonValueKind.String)
        {
            var value = stringElement.GetString();
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        return defaultValue;
    }

    private static HeaderCollection ParseHeaders(JsonElement element)
    {
        var headers = HeaderCollection.Empty;

        if (!element.TryGetProperty("headers", out var headersArray) || headersArray.ValueKind != JsonValueKind.Array)
        {
            return headers;
        }

        foreach (var headerEntry in headersArray.EnumerateArray())
        {
            if (!headerEntry.TryGetProperty("name", out var nameElement)
                || !headerEntry.TryGetProperty("value", out var valueElement)
                || nameElement.ValueKind != JsonValueKind.String
                || valueElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameElement.GetString();
            var value = valueElement.GetString();

            if (string.IsNullOrWhiteSpace(name) || value is null)
            {
                continue;
            }

            headers = headers.Add(name, value);
        }

        return headers;
    }

    private static HypertextTransferProtocolRequestData? ParseRequest(JsonElement requestElement, int maxEntryBodyBytes, out bool hasBodyBeenTruncated)
    {
        hasBodyBeenTruncated = false;

        if (!requestElement.TryGetProperty("method", out var methodElement)
            || !requestElement.TryGetProperty("url", out var urlElement))
        {
            return null;
        }

        var method = methodElement.GetString();
        var url = urlElement.GetString();

        if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var requestUri))
        {
            return null;
        }

        var version = ExtractStringOrDefault(requestElement, "httpVersion", "HTTP/1.1");
        var headers = ParseHeaders(requestElement);
        var body = ParseRequestBody(requestElement, maxEntryBodyBytes, out hasBodyBeenTruncated);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body,
            Headers = headers,
            Method = method,
            RequestUri = requestUri,
            Version = version,
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static byte[] ParseRequestBody(JsonElement requestElement, int maxEntryBodyBytes, out bool hasBodyBeenTruncated)
    {
        hasBodyBeenTruncated = false;

        if (!requestElement.TryGetProperty("postData", out var postData))
        {
            return [];
        }

        if (postData.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
        {
            return ParseRequestBodyFromPostDataText(postData, textElement.GetString(), maxEntryBodyBytes, out hasBodyBeenTruncated);
        }

        if (postData.TryGetProperty("params", out var paramsArray) && paramsArray.ValueKind == JsonValueKind.Array)
        {
            var body = ParseRequestBodyFromParams(paramsArray);
            return TruncateBodyIfNeeded(body, maxEntryBodyBytes, out hasBodyBeenTruncated);
        }

        return [];
    }

    private static byte[] ParseRequestBodyFromParams(JsonElement paramsArray)
    {
        var urlBuilder = new StringBuilder();

        foreach (var param in paramsArray.EnumerateArray())
        {
            if (!param.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameElement.GetString() ?? string.Empty;
            var value = string.Empty;

            if (param.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.String)
            {
                value = valueElement.GetString() ?? string.Empty;
            }

            if (urlBuilder.Length > 0)
            {
                urlBuilder.Append('&');
            }

            urlBuilder.Append(Uri.EscapeDataString(name));
            urlBuilder.Append('=');
            urlBuilder.Append(Uri.EscapeDataString(value));
        }

        return urlBuilder.Length > 0 ? Encoding.UTF8.GetBytes(urlBuilder.ToString()) : [];
    }

    private static byte[] ParseRequestBodyFromPostDataText(JsonElement postData, string? bodyText, int maxEntryBodyBytes, out bool hasBodyBeenTruncated)
    {
        hasBodyBeenTruncated = false;

        if (string.IsNullOrEmpty(bodyText))
        {
            return [];
        }

        if (postData.TryGetProperty("encoding", out var encodingElement)
            && encodingElement.ValueKind == JsonValueKind.String
            && string.Equals(encodingElement.GetString(), "base64", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var body = Convert.FromBase64String(bodyText);
                return TruncateBodyIfNeeded(body, maxEntryBodyBytes, out hasBodyBeenTruncated);
            }
            catch (FormatException)
            {
                return [];
            }
        }

        return ParseUtf8TextBody(bodyText, maxEntryBodyBytes, out hasBodyBeenTruncated);
    }

    private static HypertextTransferProtocolResponseData? ParseResponse(JsonElement responseElement, int maxEntryBodyBytes, out bool hasBodyBeenTruncated)
    {
        hasBodyBeenTruncated = false;

        if (!responseElement.TryGetProperty("status", out var statusElement) || statusElement.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        if (!statusElement.TryGetInt32(out var statusCode) || statusCode < 100 || statusCode > 599)
        {
            return null;
        }

        var reasonPhrase = ExtractStringOrDefault(responseElement, "statusText", string.Empty);
        var version = ExtractStringOrDefault(responseElement, "httpVersion", "HTTP/1.1");
        var headers = ParseHeaders(responseElement);
        var body = ParseResponseBody(responseElement, maxEntryBodyBytes, out hasBodyBeenTruncated);
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = reasonPhrase,
            StatusCode = statusCode,
            Version = version,
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

    private static byte[] ParseResponseBody(JsonElement responseElement, int maxEntryBodyBytes, out bool hasBodyBeenTruncated)
    {
        hasBodyBeenTruncated = false;

        if (!responseElement.TryGetProperty("content", out var content))
        {
            return [];
        }

        if (!content.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String)
        {
            return [];
        }

        var bodyText = text.GetString();

        if (string.IsNullOrEmpty(bodyText))
        {
            return [];
        }

        if (content.TryGetProperty("encoding", out var encodingElement)
            && encodingElement.ValueKind == JsonValueKind.String
            && string.Equals(encodingElement.GetString(), "base64", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var body = Convert.FromBase64String(bodyText);
                return TruncateBodyIfNeeded(body, maxEntryBodyBytes, out hasBodyBeenTruncated);
            }
            catch (FormatException)
            {
                return [];
            }
        }

        return ParseUtf8TextBody(bodyText, maxEntryBodyBytes, out hasBodyBeenTruncated);
    }

    private static byte[] ParseUtf8TextBody(string bodyText, int maxEntryBodyBytes, out bool hasBodyBeenTruncated)
    {
        if (maxEntryBodyBytes == 0)
        {
            hasBodyBeenTruncated = true;
            return [];
        }

        var utf8ByteCount = Encoding.UTF8.GetByteCount(bodyText);
        if (utf8ByteCount <= maxEntryBodyBytes)
        {
            hasBodyBeenTruncated = false;
            return Encoding.UTF8.GetBytes(bodyText);
        }

        var truncatedBody = new byte[maxEntryBodyBytes];
        var encoder = Encoding.UTF8.GetEncoder();
        encoder.Convert(bodyText.AsSpan(), truncatedBody.AsSpan(), flush: true, out _, out var bytesUsed, out _);
        if (bytesUsed < truncatedBody.Length)
        {
            var resizedBody = new byte[bytesUsed];
            Array.Copy(truncatedBody, resizedBody, bytesUsed);
            truncatedBody = resizedBody;
        }

        hasBodyBeenTruncated = true;
        return truncatedBody;
    }

    private static byte[] TruncateBodyIfNeeded(byte[] bodyBytes, int maxEntryBodyBytes, out bool hasBodyBeenTruncated)
    {
        if (bodyBytes.Length <= maxEntryBodyBytes)
        {
            hasBodyBeenTruncated = false;
            return bodyBytes;
        }

        if (maxEntryBodyBytes == 0)
        {
            hasBodyBeenTruncated = true;
            return [];
        }

        var truncatedBody = new byte[maxEntryBodyBytes];
        Array.Copy(bodyBytes, truncatedBody, maxEntryBodyBytes);
        hasBodyBeenTruncated = true;
        return truncatedBody;
    }
}
