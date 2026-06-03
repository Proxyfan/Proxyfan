using System;
using System.Text.Json;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parses Graph Query Language (GraphQL) HTTP requests per the GraphQL-over-HTTP
///     specification. Extracts the query string, optional operation name, and JSON-encoded
///     variables from a request body (POST application/json), URL query parameters (GET), or
///     raw GraphQL source (POST application/graphql). Designed to be content-type-agnostic
///     with explicit factory helpers per transport.
/// </summary>
public static class GraphQueryLanguageRequestParser
{
    /// <summary>
    ///     Parses the supplied JSON body containing fields (<c>query</c>,
    ///     <c>operationName</c>, <c>variables</c>).
    /// </summary>
    /// <param name="json">UTF-8 JSON text.</param>
    /// <returns>The parsed request, or <see langword="null" /> when the document is empty.</returns>
    /// <exception cref="System.IO.InvalidDataException">Thrown when the JSON is malformed.</exception>
    public static GraphQueryLanguageRequest? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var result = BuildFromElement(document.RootElement);
            return result;
        }
        catch (JsonException ex)
        {
            throw new System.IO.InvalidDataException("GraphQL request body is not valid JSON.", ex);
        }
    }

    /// <summary>
    ///     Builds a request from a raw query body (POST application/graphql).
    /// </summary>
    /// <param name="rawQuery">The raw query source.</param>
    /// <returns>A request capturing the query string only.</returns>
    public static GraphQueryLanguageRequest FromRawQuery(string rawQuery)
    {
        var operationName = GraphQueryLanguageOperationNameExtractor.Extract(rawQuery);
        var result = new GraphQueryLanguageRequest(rawQuery, operationName, null);
        return result;
    }

    /// <summary>
    ///     Parses GraphQL fields from a URL query string (GET requests).
    /// </summary>
    /// <param name="queryString">The URL query string (without leading '?').</param>
    /// <returns>The parsed request, or <see langword="null" /> when no query is present.</returns>
    public static GraphQueryLanguageRequest? FromUrlQuery(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        string? query = null;
        string? operationName = null;
        string? variables = null;

        var pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = pair[..separatorIndex];
            var value = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            if (string.Equals(key, "query", StringComparison.OrdinalIgnoreCase))
            {
                query = value;
            }
            else if (string.Equals(key, "operationName", StringComparison.OrdinalIgnoreCase))
            {
                operationName = value;
            }
            else if (string.Equals(key, "variables", StringComparison.OrdinalIgnoreCase))
            {
                variables = value;
            }
        }

        if (query is null)
        {
            return null;
        }

        var extractedOperationName = operationName ?? GraphQueryLanguageOperationNameExtractor.Extract(query);
        var result = new GraphQueryLanguageRequest(query, extractedOperationName, variables);
        return result;
    }

    private static GraphQueryLanguageRequest? BuildFromElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty("query", out var queryElement) || queryElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var query = queryElement.GetString();
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        string? operationName = null;
        if (element.TryGetProperty("operationName", out var operationElement) && operationElement.ValueKind == JsonValueKind.String)
        {
            operationName = operationElement.GetString();
        }

        if (string.IsNullOrEmpty(operationName))
        {
            operationName = GraphQueryLanguageOperationNameExtractor.Extract(query);
        }

        string? variables = null;
        if (element.TryGetProperty("variables", out var variablesElement) && variablesElement.ValueKind != JsonValueKind.Null && variablesElement.ValueKind != JsonValueKind.Undefined)
        {
            variables = variablesElement.GetRawText();
        }

        var result = new GraphQueryLanguageRequest(query, operationName, variables);
        return result;
    }
}
