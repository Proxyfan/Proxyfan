using System.Threading;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Observable singleton store that holds the most recently observed available update
///     (if any) and notifies subscribers whenever the value changes. Used by the periodic
///     update checker to publish results, and by UI components (e.g. the shell banner) to
///     react to availability changes.
/// </summary>
public sealed class MutableUpdateNotification
{
    /// <summary>
    ///     Raised whenever <see cref="Latest" /> changes value. Handlers receive the new
    ///     value, which may be <see langword="null" /> when the notification is cleared.
    /// </summary>
    public event UpdateNotificationChanged? Changed;

    private readonly Lock _lock;
    private UpdateInfo? _latest;

    /// <summary>
    ///     Gets the most recently observed available update, or <see langword="null" /> if
    ///     no newer version has been observed since the last clear.
    /// </summary>
    public UpdateInfo? Latest
    {
        get
        {
            lock (_lock)
            {
                return _latest;
            }
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MutableUpdateNotification" /> class.
    /// </summary>
    public MutableUpdateNotification()
    {
        var newLock = new Lock();
        _lock = newLock;
    }

    /// <summary>
    ///     Clears any current notification. Raises <see cref="Changed" /> with
    ///     <see langword="null" /> when the previous value was non-null.
    /// </summary>
    public void Clear()
    {
        Publish(null);
    }

    /// <summary>
    ///     Publishes the supplied <paramref name="update" /> as the latest known available
    ///     update. Raises <see cref="Changed" /> only when the version (or null state) has
    ///     actually changed since the previous publish.
    /// </summary>
    /// <param name="update">The available update, or <see langword="null" /> to clear.</param>
    public void Publish(UpdateInfo? update)
    {
        bool changed;
        lock (_lock)
        {
            changed = !UpdateInfoEquivalence.HasSameAvailableUpdate(_latest, update);
            _latest = update;
        }

        if (changed)
        {
            Changed?.Invoke(update);
        }
    }
}
