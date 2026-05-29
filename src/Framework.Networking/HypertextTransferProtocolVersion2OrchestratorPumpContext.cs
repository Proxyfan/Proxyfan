using System.IO;
using System.IO.Pipelines;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles the per-direction pump arguments used by
///     <see cref="HypertextTransferProtocolVersion2Orchestrator" />. Keeps the orchestrator's
///     internal pump method below the analyzer's four-parameter limit (ATXCS022).
/// </summary>
public sealed class HypertextTransferProtocolVersion2OrchestratorPumpContext
{
    /// <summary>
    ///     Gets a description of the client endpoint used when constructing traffic flows.
    /// </summary>
    public required string ClientEndPointDescription { get; init; }

    /// <summary>
    ///     Gets the direction frames flowing through this pump are taking.
    /// </summary>
    public required HypertextTransferProtocolVersion2RelayDirection Direction { get; init; }

    /// <summary>
    ///     Gets the pipe reader the pump consumes frames from.
    /// </summary>
    public required PipeReader Reader { get; init; }

    /// <summary>
    ///     Gets the destination stream the pump forwards frames to.
    /// </summary>
    public required Stream WriteStream { get; init; }
}
