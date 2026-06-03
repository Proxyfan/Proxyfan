using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub <see cref="IResponsePhaseRule" /> that records how many times it was
///     evaluated by the rule engine. Returns no action so it never modifies the response.
/// </summary>
public sealed class RecordingResponsePhaseRule : IResponsePhaseRule
{
    /// <summary>
    ///     Gets the number of times <see cref="EvaluateResponse" /> has been invoked.
    /// </summary>
    public int EvaluationCount { get; private set; }

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public ResponsePipelineAction? EvaluateResponse(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        EvaluationCount++;
        return null;
    }
}
