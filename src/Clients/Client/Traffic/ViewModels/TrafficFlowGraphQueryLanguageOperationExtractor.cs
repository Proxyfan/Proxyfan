using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using System;
using System.IO;
using System.Text;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Extracts the Graph Query Language (GraphQL) operation name from a captured request
///     for surfacing in the traffic list column. Returns <see langword="null" /> when the
///     request is not GraphQL or the operation name cannot be determined.
/// </summary>
public static class TrafficFlowGraphQueryLanguageOperationExtractor
{
    /// <summary>
    ///     Returns the operation name from the captured request, or <see langword="null" />.
    /// </summary>
    /// <param name="request">The captured HTTP request (may be <see langword="null" />).</param>
    /// <returns>The operation name or <see langword="null" />.</returns>
    public static string? Extract(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return null;
        }

        var contentTypeHeader = request.Headers.Get("Content-Type");
        var path = request.RequestUri.AbsolutePath;
        if (!GraphQueryLanguageRequestDetector.HasIndicator(path, contentTypeHeader))
        {
            return null;
        }

        try
        {
            if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var query = request.RequestUri.Query;
                if (query.StartsWith('?'))
                {
                    query = query[1..];
                }

                var fromUrl = GraphQueryLanguageRequestParser.FromUrlQuery(query);
                return fromUrl?.OperationName;
            }

            if (request.Body.IsEmpty)
            {
                return null;
            }

            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            if (!string.IsNullOrWhiteSpace(contentTypeHeader)
                && contentTypeHeader.Contains("application/graphql", StringComparison.OrdinalIgnoreCase)
                && !contentTypeHeader.Contains("+json", StringComparison.OrdinalIgnoreCase)
                && !contentTypeHeader.Contains("-response+json", StringComparison.OrdinalIgnoreCase))
            {
                var fromRaw = GraphQueryLanguageRequestParser.FromRawQuery(bodyText);
                return fromRaw.OperationName;
            }

            var parsed = GraphQueryLanguageRequestParser.FromJson(bodyText);
            return parsed?.OperationName;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
