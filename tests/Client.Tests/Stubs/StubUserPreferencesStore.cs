using Proxyfan.Domain.Configuration;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     In-memory <see cref="IUserPreferencesStore" /> used to drive view-model tests without
///     touching the file system. Captures every save call.
/// </summary>
public sealed class StubUserPreferencesStore : IUserPreferencesStore
{
    /// <summary>
    ///     Gets the most recently saved preferences, or <see langword="null" /> when
    ///     <see cref="Save" /> has not yet been invoked.
    /// </summary>
    public UserPreferences? LastSaved { get; private set; }

    /// <summary>
    ///     Gets or sets the preferences returned from the next call to <see cref="Load" />.
    /// </summary>
    public UserPreferences PreferencesToLoad { get; set; } = UserPreferencesDefaults.Create();

    /// <summary>
    ///     Gets the number of times <see cref="Save" /> has been invoked.
    /// </summary>
    public int SaveCallCount { get; private set; }

    /// <inheritdoc />
    public UserPreferences Load()
    {
        return PreferencesToLoad;
    }

    /// <inheritdoc />
    public void Save(UserPreferences preferences)
    {
        LastSaved = preferences;
        SaveCallCount++;
    }
}
