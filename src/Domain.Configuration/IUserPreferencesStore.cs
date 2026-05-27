namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Persistence abstraction for <see cref="UserPreferences" />. Implementations may write a
///     JSON/YAML file on disk, hold preferences in memory for tests, or roundtrip them to any
///     other backing store. The UI uses this to load on open and save on apply.
/// </summary>
public interface IUserPreferencesStore
{
    /// <summary>
    ///     Loads the persisted preferences. Returns <see cref="UserPreferencesDefaults.Create" />
    ///     when no preferences have been stored or the stored data is corrupt.
    /// </summary>
    /// <returns>The current preferences.</returns>
    UserPreferences Load();

    /// <summary>
    ///     Persists the supplied preferences, replacing any previously stored preferences.
    /// </summary>
    /// <param name="preferences">The preferences to persist.</param>
    void Save(UserPreferences preferences);
}
