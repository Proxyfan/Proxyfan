using Proxyfan.Domain.Traffic;
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
    public Task<Guid> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        CancellationToken cancellationToken)
    {
        SingleInvocations.Add(originalRequest);
        return Task.FromResult(Guid.NewGuid());
    }

    /// <inheritdoc />
    public Task<int> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        int repeatCount,
        TimeSpan delayBetweenRepeats,
        CancellationToken cancellationToken)
    {
        MultiInvocations.Add((originalRequest, repeatCount, delayBetweenRepeats));
        return Task.FromResult(repeatCount);
    }
}
