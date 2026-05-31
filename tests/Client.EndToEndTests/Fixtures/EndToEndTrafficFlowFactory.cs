using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Client.EndToEndTests.Fixtures;

/// <summary>
///     Deterministic factory for fully-formed <see cref="TrafficFlow" /> instances
///     used as input to end-to-end UI tests. All flows have stable IDs and
///     timestamps derived from a fixed seed so test assertions about ordering
///     and counts remain reproducible.
/// </summary>
internal static class EndToEndTrafficFlowFactory
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     Creates a completed GET flow against <paramref name="url" /> with a 200 OK
    ///     response. The flow's <see cref="TrafficFlow.Id" /> is derived deterministically
    ///     from <paramref name="seed" />.
    /// </summary>
    /// <param name="seed">Numeric seed used for the flow GUID and timestamp offset.</param>
    /// <param name="url">The request URL.</param>
    /// <returns>The constructed flow.</returns>
    public static TrafficFlow CreateCompletedGet(int seed, string url)
    {
        var id = new Guid(seed, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);
        var startedAt = BaseTime.AddSeconds(seed);
        var flow = new TrafficFlow(id, "127.0.0.1:54321", startedAt);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        flow.SetResponse(response);
        flow.Complete();
        return flow;
    }

    /// <summary>
    ///     Creates a still-pending flow (no request set yet) — useful to assert
    ///     placeholder rendering in the traffic list.
    /// </summary>
    /// <param name="seed">Numeric seed used for the flow GUID and timestamp offset.</param>
    /// <returns>The constructed flow.</returns>
    public static TrafficFlow CreatePending(int seed)
    {
        var id = new Guid(seed, 0, 0, [0, 0, 0, 0, 0, 0, 0, 1]);
        var startedAt = BaseTime.AddSeconds(seed);
        var flow = new TrafficFlow(id, "127.0.0.1:54321", startedAt);
        return flow;
    }
}
