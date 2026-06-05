using Proxyfan.Domain.Traffic;
using Proxyfan.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     A stub <see cref="IRequestRepeater" /> for view-model tests that records each
///     invocation. The stub never reaches the network or the rule engine.
/// </summary>
public sealed class StubRequestRepeater : IRequestRepeater
{
    /// <summary>
    ///     Gets the list of single-shot repeat invocations.
    /// </summary>
    public List<HypertextTransferProtocolRequestData> SingleInvocations { get; } = [];

    /// <summary>
    ///     Gets the list of multi-shot repeat invocations.
    /// </summary>
    public List<(HypertextTransferProtocolRequestData Request, int Count, TimeSpan Delay)> MultiInvocations { get; } = [];

    /// <inheritdoc />
    public Task<Result<Guid>> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        CancellationToken cancellationToken)
    {
        SingleInvocations.Add(originalRequest);
        return Task.FromResult(Result.Success(Guid.NewGuid()));
    }

    /// <inheritdoc />
    public Task<Result<RequestReplayBatchResult>> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        int repeatCount,
        TimeSpan delayBetweenRepeats,
        CancellationToken cancellationToken)
    {
        MultiInvocations.Add((originalRequest, repeatCount, delayBetweenRepeats));
        var result = new RequestReplayBatchResult(repeatCount, repeatCount);
        return Task.FromResult(Result.Success(result));
    }
}
