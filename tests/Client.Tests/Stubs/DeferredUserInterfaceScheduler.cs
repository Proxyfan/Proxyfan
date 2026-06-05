using Proxyfan.Presentation.Threading;
using System.Collections.Generic;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Test implementation of <see cref="IUserInterfaceScheduler" /> that queues
///     scheduled work items instead of executing them immediately. Call
///     <see cref="DrainQueue" /> to run all pending items in FIFO order.
/// </summary>
internal sealed class DeferredUserInterfaceScheduler : IUserInterfaceScheduler
{
    private readonly Queue<UserInterfaceWorkItem> _queue = new();

    /// <inheritdoc />
    public bool HasAccess()
    {
        return true;
    }

    /// <inheritdoc />
    public void Post(UserInterfaceWorkItem action)
    {
        _queue.Enqueue(action);
    }

    /// <summary>
    ///     Executes all pending work items in the order they were posted, then clears
    ///     the queue.
    /// </summary>
    public void DrainQueue()
    {
        while (_queue.Count > 0)
        {
            _queue.Dequeue()();
        }
    }
}
