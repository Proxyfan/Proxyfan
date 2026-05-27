using Proxyfan.Domain.Configuration;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     File-backed <see cref="IUserPreferencesStore" />. Reads from and writes to a JSON file
///     using <see cref="UserPreferencesJsonSerializer" />. This is the production store used by
///     the Preferences tool to persist user settings to <c>%LOCALAPPDATA%\Proxyfan</c>.
/// </summary>
public sealed class FileUserPreferencesStore : IUserPreferencesStore
{
    private readonly string _filePath;

    /// <summary>
    ///     Initializes a new <see cref="FileUserPreferencesStore" /> backed by the supplied file
    ///     path.
    /// </summary>
    /// <param name="filePath">The absolute path to the preferences JSON file.</param>
    public FileUserPreferencesStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc />
    public UserPreferences Load()
    {
        return UserPreferencesJsonSerializer.ReadFromFile(_filePath);
    }

    /// <inheritdoc />
    public void Save(UserPreferences preferences)
    {
        UserPreferencesJsonSerializer.WriteToFile(_filePath, preferences);
    }
}
