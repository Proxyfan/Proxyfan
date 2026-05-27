using Proxyfan.Presentation.Threading;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Test implementation of <see cref="IUserInterfaceScheduler" /> that invokes
///     scheduled work synchronously on the calling thread. Removes any UI-thread
///     marshaling from view-model tests.
/// </summary>
internal sealed class InlineUserInterfaceScheduler : IUserInterfaceScheduler
{
    /// <summary>
    ///     Gets a shared, thread-safe singleton instance.
    /// </summary>
    public static InlineUserInterfaceScheduler Instance { get; } = new();

    /// <inheritdoc />
    public bool HasAccess()
    {
        return true;
    }

    /// <inheritdoc />
    public void Post(UserInterfaceWorkItem action)
    {
        action();
    }
}
