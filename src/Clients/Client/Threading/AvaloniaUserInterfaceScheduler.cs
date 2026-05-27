using Avalonia.Threading;
using Proxyfan.Presentation.Threading;

namespace Proxyfan.Client.Threading;

/// <summary>
///     Avalonia-backed <see cref="IUserInterfaceScheduler" /> that delegates to
///     <see cref="Dispatcher.UIThread" /> for marshaling work onto the UI thread.
/// </summary>
public sealed class AvaloniaUserInterfaceScheduler : IUserInterfaceScheduler
{
    /// <inheritdoc />
    public bool HasAccess()
    {
        return Dispatcher.UIThread.CheckAccess();
    }

    /// <inheritdoc />
    public void Post(UserInterfaceWorkItem action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(() => action());
    }
}
