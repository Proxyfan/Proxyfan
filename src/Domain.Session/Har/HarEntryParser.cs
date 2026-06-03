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
        var startedAt = ExtractStartedDateTime(entry);
        var clientEndPoint = ExtractClientEndPoint(entry);
        var flowId = ExtractFlowId(entry);
        var flow = new TrafficFlow(flowId, clientEndPoint, startedAt);

        if (entry.TryGetProperty("request", out var requestElement))
        {
            var request = ParseRequest(requestElement);

            if (request is not null)
            {
                flow.SetRequest(request);
            }
        }

        if (entry.TryGetProperty("response", out var responseElement))
        {
            var response = ParseResponse(responseElement);

            if (response is not null && flow.Status == TrafficFlowStatus.Active)
            {
                flow.SetResponse(response);
                flow.Complete();
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

    private static HypertextTransferProtocolRequestData? ParseRequest(JsonElement requestElement)
    {
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
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = method,
            RequestUri = requestUri,
            Version = version,
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData? ParseResponse(JsonElement responseElement)
    {
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
        var body = ParseResponseBody(responseElement);
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

    private static byte[] ParseResponseBody(JsonElement responseElement)
    {
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
                return Convert.FromBase64String(bodyText);
            }
            catch (FormatException)
            {
                return [];
            }
        }

        return Encoding.UTF8.GetBytes(bodyText);
    }
}
