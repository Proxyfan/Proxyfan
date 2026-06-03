using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Filters traffic flows by a free-text query against URL, method, status, and host.
/// </summary>
public sealed class TrafficFilter
{
    private readonly string _query;

    /// <summary>
    ///     Initializes a new <see cref="TrafficFilter" />.
    /// </summary>
    /// <param name="query">The query string. Empty matches everything.</param>
    public TrafficFilter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _query = string.Empty;
        }
        else
        {
            _query = query.Trim();
        }
    }

    /// <summary>
    ///     Returns the supplied flows that match the filter, preserving order.
    /// </summary>
    /// <param name="flows">The flows to filter.</param>
    /// <returns>The matching subset.</returns>
    public IReadOnlyList<TrafficFlow> Apply(IReadOnlyList<TrafficFlow> flows)
    {
        if (_query.Length == 0)
        {
            return flows;
        }

        var matches = new List<TrafficFlow>(flows.Count);

        for (var index = 0; index < flows.Count; index++)
        {
            var flow = flows[index];

            if (HasMatch(flow))
            {
                matches.Add(flow);
            }
        }

        return matches;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the flow matches the filter query.
    /// </summary>
    /// <param name="flow">The flow to test.</param>
    /// <returns><see langword="true" /> when matched.</returns>
    public bool HasMatch(TrafficFlow flow)
    {
        if (_query.Length == 0)
        {
            return true;
        }

        if (HasRequestMatch(flow.Request))
        {
            return true;
        }

        if (HasResponseMatch(flow.Response))
        {
            return true;
        }

        if (HasAnnotationMatch(flow))
        {
            return true;
        }

        return false;
    }

    private bool HasAnnotationMatch(TrafficFlow flow)
    {
        if (!string.IsNullOrEmpty(flow.Comment) && flow.Comment.Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (flow.ColorTag != TrafficFlowColorTag.None
            && flow.ColorTag.ToString().Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private bool HasRequestMatch(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return false;
        }

        if (request.RequestUri.ToString().Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (request.Method.Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var hostHeader = request.Headers.Get("Host");

        if (!string.IsNullOrEmpty(hostHeader) && hostHeader.Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private bool HasResponseMatch(HypertextTransferProtocolResponseData? response)
    {
        if (response is null)
        {
            return false;
        }

        if (response.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(response.ReasonPhrase) && response.ReasonPhrase.Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
