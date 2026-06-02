namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Stable definition of a built-in throttle preset: a culture-invariant
///     identifier paired with the resource key for the localized label and a
///     factory for the underlying profile.
/// </summary>
public sealed class ThrottlePresetDefinition
{
    /// <summary>
    ///     Gets the stable culture-invariant identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Gets the factory that produces the underlying throttle profile, or
    ///     returns <see langword="null" /> for the "disabled" entry.
    /// </summary>
    public ThrottlePresetFactory ProfileFactory { get; }

    /// <summary>
    ///     Gets the resource key used to resolve the localized display name.
    /// </summary>
    public string ResourceKey { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThrottlePresetDefinition" />.
    /// </summary>
    /// <param name="identifier">The stable identifier.</param>
    /// <param name="resourceKey">The display-name resource key.</param>
    /// <param name="profileFactory">The factory that produces the underlying profile.</param>
    public ThrottlePresetDefinition(string identifier, string resourceKey, ThrottlePresetFactory profileFactory)
    {
        Identifier = identifier;
        ResourceKey = resourceKey;
        ProfileFactory = profileFactory;
    }
}
