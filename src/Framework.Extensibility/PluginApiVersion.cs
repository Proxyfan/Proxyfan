namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Parsed major + minor version pair, returned by <see cref="PluginApiVersionParser" />.
/// </summary>
public sealed class PluginApiVersion
{
    /// <summary>
    ///     Gets the major version.
    /// </summary>
    public int Major { get; }

    /// <summary>
    ///     Gets the minor version.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginApiVersion" />.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    public PluginApiVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }
}
