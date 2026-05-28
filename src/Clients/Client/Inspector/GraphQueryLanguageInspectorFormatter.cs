using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Formats a captured HTTP request as a human-readable Graph Query Language (GraphQL)
///     inspector view. Returns an empty string when the request is not detected as GraphQL,
///     or a helpful diagnostic when GraphQL is detected but the payload cannot be parsed.
/// </summary>
public static class GraphQueryLanguageInspectorFormatter
{
    /// <summary>
    ///     Renders the GraphQL inspector text for the supplied request, or an empty string
    ///     when the request is not GraphQL.
    /// </summary>
    /// <param name="request">The captured HTTP request data (may be <see langword="null" />).</param>
    /// <returns>The GraphQL inspector text or <see cref="string.Empty" />.</returns>
    public static string Format(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return string.Empty;
        }

        var contentTypeHeader = request.Headers.Get("Content-Type");
        var urlPath = request.RequestUri.AbsolutePath;
        if (!GraphQueryLanguageRequestDetector.HasIndicator(urlPath, contentTypeHeader))
        {
            return string.Empty;
        }

        var parsed = TryParse(request, contentTypeHeader);
        if (parsed is null)
        {
            return "GraphQL request detected, but the payload could not be parsed.";
        }

        return RenderRequest(parsed);
    }

    private static string FormatVariables(string variables)
    {
        return JsonPrettyPrinter.PrettyPrint(variables);
    }

    private static string RenderRequest(GraphQueryLanguageRequest parsed)
    {
        var builder = new StringBuilder();
        var operationName = string.IsNullOrEmpty(parsed.OperationName) ? "(anonymous)" : parsed.OperationName;
        builder.Append(CultureInfo.InvariantCulture, $"Operation: {operationName}");
        builder.Append('\n');
        builder.Append('\n');
        builder.Append("Query:");
        builder.Append('\n');
        builder.Append(parsed.Query);

        if (!string.IsNullOrEmpty(parsed.Variables))
        {
            builder.Append('\n');
            builder.Append('\n');
            builder.Append("Variables:");
            builder.Append('\n');
            builder.Append(FormatVariables(parsed.Variables));
        }

        return builder.ToString();
    }

    private static GraphQueryLanguageRequest? TryParse(HypertextTransferProtocolRequestData request, string? contentTypeHeader)
    {
        try
        {
            if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var query = request.RequestUri.Query;
                if (query.StartsWith('?'))
                {
                    query = query[1..];
                }

                return GraphQueryLanguageRequestParser.FromUrlQuery(query);
            }

            if (request.Body.IsEmpty)
            {
                return null;
            }

            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            if (string.IsNullOrWhiteSpace(contentTypeHeader))
            {
                return GraphQueryLanguageRequestParser.FromJson(bodyText);
            }

            if (contentTypeHeader.Contains("application/graphql", StringComparison.OrdinalIgnoreCase) &&
                !contentTypeHeader.Contains("+json", StringComparison.OrdinalIgnoreCase) &&
                !contentTypeHeader.Contains("-response+json", StringComparison.OrdinalIgnoreCase))
            {
                return GraphQueryLanguageRequestParser.FromRawQuery(bodyText);
            }

            return GraphQueryLanguageRequestParser.FromJson(bodyText);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
