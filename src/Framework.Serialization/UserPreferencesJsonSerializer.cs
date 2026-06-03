using Proxyfan.Domain.Configuration;
using System.IO;
using System.Text.Json;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Serializes <see cref="UserPreferences" /> to and from a JSON file on disk. The file
///     format carries a <c>schemaVersion</c> field so future versions can detect and upgrade
///     older formats. Used by the Preferences tool to persist user-editable settings across
///     application launches.
/// </summary>
public static class UserPreferencesJsonSerializer
{
    /// <summary>
    ///     The current schema version embedded in the serialized JSON.
    /// </summary>
    public const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions Options;

    static UserPreferencesJsonSerializer()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        Options = options;
    }

    /// <summary>
    ///     Deserializes a <see cref="UserPreferences" /> instance from JSON text. Returns the
    ///     defaults when the JSON is empty, the schema version is unknown, or the payload is
    ///     malformed. Fields that fall outside the valid ranges enforced by the Preferences UI
    ///     (proxy port 1024-65535, upstream proxy port 1-65535, capture cap 100-1,000,000) are
    ///     replaced with their documented defaults so a hand-edited or corrupted file cannot
    ///     start the application with out-of-range settings.
    /// </summary>
    /// <param name="json">The JSON text to deserialize.</param>
    /// <returns>The deserialized preferences (or defaults on failure).</returns>
    public static UserPreferences Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return UserPreferencesDefaults.Create();
        }

        UserPreferencesFile? file;
        try
        {
            file = JsonSerializer.Deserialize<UserPreferencesFile>(json, Options);
        }
        catch (JsonException)
        {
            return UserPreferencesDefaults.Create();
        }

        if (file is null || file.SchemaVersion != CurrentSchemaVersion || file.Preferences is null)
        {
            return UserPreferencesDefaults.Create();
        }

        var raw = file.Preferences;
        var defaults = UserPreferencesDefaults.Create();
        var loaded = new UserPreferences
        {
            CaptureMaximumFlows = SanitizeCaptureMaximumFlows(raw.CaptureMaximumFlows, defaults.CaptureMaximumFlows),
            IsRegisterSystemProxyOnStartup = raw.IsRegisterSystemProxyOnStartup ?? defaults.IsRegisterSystemProxyOnStartup,
            IsStartProxyOnLaunch = raw.IsStartProxyOnLaunch ?? defaults.IsStartProxyOnLaunch,
            IsUpstreamProxyEnabled = raw.IsUpstreamProxyEnabled ?? defaults.IsUpstreamProxyEnabled,
            Locale = raw.Locale,
            LogLevel = raw.LogLevel ?? defaults.LogLevel,
            ProxyPort = SanitizeProxyPort(raw.ProxyPort, defaults.ProxyPort),
            Theme = raw.Theme ?? defaults.Theme,
            UpstreamProxyHost = raw.UpstreamProxyHost,
            UpstreamProxyPort = SanitizeUpstreamProxyPort(raw.UpstreamProxyPort, defaults.UpstreamProxyPort),
        };
        return loaded;
    }

    /// <summary>
    ///     Reads and deserializes preferences from the supplied file path. Returns the defaults
    ///     when the file does not exist.
    /// </summary>
    /// <param name="filePath">The absolute path to the preferences file.</param>
    /// <returns>The deserialized preferences.</returns>
    public static UserPreferences ReadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return UserPreferencesDefaults.Create();
        }

        var json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    /// <summary>
    ///     Serializes the supplied preferences to JSON text with the current schema version.
    /// </summary>
    /// <param name="preferences">The preferences to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(UserPreferences preferences)
    {
        var raw = new RawUserPreferences
        {
            CaptureMaximumFlows = preferences.CaptureMaximumFlows,
            IsRegisterSystemProxyOnStartup = preferences.IsRegisterSystemProxyOnStartup,
            IsStartProxyOnLaunch = preferences.IsStartProxyOnLaunch,
            IsUpstreamProxyEnabled = preferences.IsUpstreamProxyEnabled,
            Locale = preferences.Locale,
            LogLevel = preferences.LogLevel,
            ProxyPort = preferences.ProxyPort,
            Theme = preferences.Theme,
            UpstreamProxyHost = preferences.UpstreamProxyHost,
            UpstreamProxyPort = preferences.UpstreamProxyPort,
        };
        var file = new UserPreferencesFile
        {
            Preferences = raw,
            SchemaVersion = CurrentSchemaVersion,
        };
        var json = JsonSerializer.Serialize(file, Options);
        return json;
    }

    /// <summary>
    ///     Serializes the supplied preferences and writes them to the supplied file path,
    ///     creating any missing parent directories.
    /// </summary>
    /// <param name="filePath">The absolute path to write to.</param>
    /// <param name="preferences">The preferences to write.</param>
    public static void WriteToFile(string filePath, UserPreferences preferences)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = Serialize(preferences);
        File.WriteAllText(filePath, json);
    }

    private static int SanitizeCaptureMaximumFlows(int? value, int fallback)
    {
        if (value is null || !UserPreferencesValidation.HasValidCaptureMaximumFlows(value.Value))
        {
            return fallback;
        }

        return value.Value;
    }

    private static int SanitizeProxyPort(int? value, int fallback)
    {
        if (value is null or < 1024 or > 65535)
        {
            return fallback;
        }

        return value.Value;
    }

    private static int SanitizeUpstreamProxyPort(int? value, int fallback)
    {
        if (value is null or < 1 or > 65535)
        {
            return fallback;
        }

        return value.Value;
    }

    private sealed class RawUserPreferences
    {
        public int? CaptureMaximumFlows { get; set; }

        public bool? IsRegisterSystemProxyOnStartup { get; set; }

        public bool? IsStartProxyOnLaunch { get; set; }

        public bool? IsUpstreamProxyEnabled { get; set; }

        public string? Locale { get; set; }

        public string? LogLevel { get; set; }

        public int? ProxyPort { get; set; }

        public string? Theme { get; set; }

        public string? UpstreamProxyHost { get; set; }

        public int? UpstreamProxyPort { get; set; }
    }

    private sealed class UserPreferencesFile
    {
        public RawUserPreferences? Preferences { get; set; }

        public int SchemaVersion { get; set; }
    }
}
