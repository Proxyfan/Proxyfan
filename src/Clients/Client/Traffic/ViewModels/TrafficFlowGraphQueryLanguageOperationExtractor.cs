using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Columns;

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
        return GraphQueryLanguageRequestClassifier.GetOperationName(request);
    }
}
