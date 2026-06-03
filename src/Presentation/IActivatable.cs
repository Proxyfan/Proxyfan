namespace Proxyfan.Presentation;

/// <summary>
///     Marker contract for view models that support a one-time activation step
///     triggered when their host view is first loaded into the visual tree.
///     Implementations must be idempotent — a second call to <see cref="Activate" />
///     must be a no-op.
/// </summary>
public interface IActivatable
{
    /// <summary>
    ///     Runs one-time activation logic. Must be idempotent.
    /// </summary>
    void Activate();
}
