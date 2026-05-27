namespace Proxyfan.Presentation.Threading;

/// <summary>
///     Abstraction for marshaling work onto the UI thread. Enables view models to
///     post UI updates without taking a direct dependency on Avalonia's dispatcher,
///     which keeps unit tests free of UI-thread initialization concerns.
/// </summary>
public interface IUserInterfaceScheduler
{
    /// <summary>
    ///     Returns whether the calling thread is the UI thread.
    /// </summary>
    /// <returns>True when the caller is already on the UI thread.</returns>
    bool HasAccess();

    /// <summary>
    ///     Posts the supplied action to run on the UI thread. May execute synchronously
    ///     when the caller is already on the UI thread, depending on the implementation.
    /// </summary>
    /// <param name="action">The work to perform on the UI thread.</param>
    void Post(UserInterfaceWorkItem action);
}
