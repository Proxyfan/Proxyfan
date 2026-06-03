using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Proxyfan.Domain.Session.Har;

/// <summary>
///     Static writer for HAR 1.2 document elements. Pure functional helpers that emit JSON
///     structures to a <see cref="Utf8JsonWriter" /> without owning the writer's lifetime.
/// </summary>
public static class HarDocumentWriter
{
    private const string CreatorName = "Proxyfan";
    private const string CreatorVersion = "1.0";
    private const string HarVersion = "1.2";

    /// <summary>
    ///     Determines whether a MIME type indicates text-like content that should be serialized
    ///     into the <c>content.text</c> field.
    /// </summary>
    /// <param name="mimeType">The MIME type to evaluate.</param>
    /// <returns><see langword="true" /> when the MIME type is text-like.</returns>
    public static bool HasTextLikeMimeType(string mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            return false;
        }

        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("javascript", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Writes the root <c>log</c> object including version, creator, and entries array.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="flows">The traffic flows to serialize as entries.</param>
    /// <param name="cancellationToken">Token observed between entries to allow prompt cancellation of large exports.</param>
    public static void WriteLog(Utf8JsonWriter writer, IReadOnlyList<TrafficFlow> flows, CancellationToken cancellationToken)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("log");
        writer.WriteString("version", HarVersion);
        WriteCreator(writer);
        WriteEntries(writer, flows, cancellationToken);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteCache(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("cache");
        writer.WriteEndObject();
    }

    private static void WriteContent(Utf8JsonWriter writer, HypertextTransferProtocolResponseData response)
    {
        writer.WriteStartObject("content");
        writer.WriteNumber("size", response.Body.Length);
        var mimeType = response.Headers.Get("Content-Type") ?? string.Empty;
        writer.WriteString("mimeType", mimeType);

        if (response.Body.Length > 0 && HasTextLikeMimeType(mimeType))
        {
            writer.WriteString("text", Encoding.UTF8.GetString(response.Body.Span));
        }

        writer.WriteEndObject();
    }

    private static void WriteCookies(Utf8JsonWriter writer, HeaderCollection headers)
    {
        writer.WriteStartArray("cookies");
        var cookieValue = headers.Get("Cookie") ?? headers.Get("Set-Cookie");

        if (!string.IsNullOrWhiteSpace(cookieValue))
        {
            var pairs = cookieValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var pair in pairs)
            {
                var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);

                if (separatorIndex <= 0)
                {
                    continue;
                }

                var name = pair[..separatorIndex].Trim();
                var value = pair[(separatorIndex + 1)..].Trim();
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteString("value", value);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteCreator(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("creator");
        writer.WriteString("name", CreatorName);
        writer.WriteString("version", CreatorVersion);
        writer.WriteEndObject();
    }

    private static void WriteEmptyRequest(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("request");
        writer.WriteString("method", string.Empty);
        writer.WriteString("url", string.Empty);
        writer.WriteString("httpVersion", string.Empty);
        writer.WriteStartArray("cookies");
        writer.WriteEndArray();
        writer.WriteStartArray("headers");
        writer.WriteEndArray();
        writer.WriteStartArray("queryString");
        writer.WriteEndArray();
        writer.WriteNumber("headersSize", -1);
        writer.WriteNumber("bodySize", 0);
        writer.WriteEndObject();
    }

    private static void WriteEmptyResponse(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("response");
        writer.WriteNumber("status", 0);
        writer.WriteString("statusText", string.Empty);
        writer.WriteString("httpVersion", string.Empty);
        writer.WriteStartArray("cookies");
        writer.WriteEndArray();
        writer.WriteStartArray("headers");
        writer.WriteEndArray();
        writer.WriteStartObject("content");
        writer.WriteNumber("size", 0);
        writer.WriteString("mimeType", string.Empty);
        writer.WriteEndObject();
        writer.WriteString("redirectURL", string.Empty);
        writer.WriteNumber("headersSize", -1);
        writer.WriteNumber("bodySize", 0);
        writer.WriteEndObject();
    }

    private static void WriteEntries(Utf8JsonWriter writer, IReadOnlyList<TrafficFlow> flows, CancellationToken cancellationToken)
    {
        writer.WriteStartArray("entries");

        foreach (var flow in flows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteEntry(writer, flow, cancellationToken);
        }

        writer.WriteEndArray();
    }

    private static void WriteEntry(Utf8JsonWriter writer, TrafficFlow flow, CancellationToken cancellationToken)
    {
        writer.WriteStartObject();
        writer.WriteString("startedDateTime", flow.StartedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        var durationMilliseconds = flow.Timings.TotalDuration?.TotalMilliseconds ?? 0;
        writer.WriteNumber("time", durationMilliseconds);

        if (flow.Request is not null)
        {
            WriteRequest(writer, flow.Request);
        }
        else
        {
            WriteEmptyRequest(writer);
        }

        if (flow.Response is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteResponse(writer, flow.Response);
        }
        else
        {
            WriteEmptyResponse(writer);
        }

        WriteCache(writer);
        WriteTimings(writer, flow);
        writer.WriteString("_proxyfanFlowId", flow.Id.ToString());
        writer.WriteString("_proxyfanClientEndPoint", flow.ClientEndPoint);
        writer.WriteString("_proxyfanStatus", flow.Status.ToString());
        if (flow.ColorTag != TrafficFlowColorTag.None)
        {
            writer.WriteString("_proxyfanColorTag", flow.ColorTag.ToString());
        }
        if (flow.Comment is not null)
        {
            writer.WriteString("_proxyfanComment", flow.Comment);
        }
        writer.WriteEndObject();
    }

    private static void WriteHeaders(Utf8JsonWriter writer, HeaderCollection headers)
    {
        writer.WriteStartArray("headers");

        foreach (KeyValuePair<string, string[]> header in headers)
        {
            foreach (var value in header.Value)
            {
                writer.WriteStartObject();
                writer.WriteString("name", header.Key);
                writer.WriteString("value", value);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteQueryString(Utf8JsonWriter writer, Uri requestUri)
    {
        writer.WriteStartArray("queryString");

        if (requestUri.IsAbsoluteUri && !string.IsNullOrEmpty(requestUri.Query))
        {
            var queryWithoutPrefix = requestUri.Query.TrimStart('?');
            var pairs = queryWithoutPrefix.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
                writer.WriteStartObject();

                if (separatorIndex < 0)
                {
                    writer.WriteString("name", pair);
                    writer.WriteString("value", string.Empty);
                }
                else
                {
                    writer.WriteString("name", pair[..separatorIndex]);
                    writer.WriteString("value", pair[(separatorIndex + 1)..]);
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteRequest(Utf8JsonWriter writer, HypertextTransferProtocolRequestData request)
    {
        writer.WriteStartObject("request");
        writer.WriteString("method", request.Method);
        writer.WriteString("url", request.RequestUri.ToString());
        writer.WriteString("httpVersion", request.Version);
        WriteCookies(writer, request.Headers);
        WriteHeaders(writer, request.Headers);
        WriteQueryString(writer, request.RequestUri);
        writer.WriteNumber("headersSize", -1);
        writer.WriteNumber("bodySize", request.Body.Length);
        writer.WriteEndObject();
    }

    private static void WriteResponse(Utf8JsonWriter writer, HypertextTransferProtocolResponseData response)
    {
        writer.WriteStartObject("response");
        writer.WriteNumber("status", response.StatusCode);
        writer.WriteString("statusText", response.ReasonPhrase);
        writer.WriteString("httpVersion", response.Version);
        WriteCookies(writer, response.Headers);
        WriteHeaders(writer, response.Headers);
        WriteContent(writer, response);
        writer.WriteString("redirectURL", response.Headers.Get("Location") ?? string.Empty);
        writer.WriteNumber("headersSize", -1);
        writer.WriteNumber("bodySize", response.Body.Length);
        writer.WriteEndObject();
    }

    private static void WriteTimings(Utf8JsonWriter writer, TrafficFlow flow)
    {
        writer.WriteStartObject("timings");
        writer.WriteNumber("send", 0);
        var waitMilliseconds = flow.Timings.RequestCompletedAt.HasValue && flow.Timings.ResponseStartedAt.HasValue
            ? (flow.Timings.ResponseStartedAt.Value - flow.Timings.RequestCompletedAt.Value).TotalMilliseconds
            : 0;
        writer.WriteNumber("wait", waitMilliseconds);
        var receiveMilliseconds = flow.Timings.ResponseStartedAt.HasValue && flow.Timings.ResponseCompletedAt.HasValue
            ? (flow.Timings.ResponseCompletedAt.Value - flow.Timings.ResponseStartedAt.Value).TotalMilliseconds
            : 0;
        writer.WriteNumber("receive", receiveMilliseconds);
        writer.WriteEndObject();
    }
}
