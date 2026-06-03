namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Presentation-facing abstraction over runtime throttling state.
/// </summary>
public interface IThrottleCoordinator
{
    /// <summary>
    ///     Raised when the active profile changes.
    /// </summary>
    event ThrottleCoordinatorProfileChanged? ProfileChanged;

    /// <summary>
    ///     Gets the currently active profile identifier, or <see langword="null" /> when throttling is disabled.
    /// </summary>
    string? ActiveProfileIdentifier { get; }

    /// <summary>
    ///     Applies the supplied preset identifier.
    /// </summary>
    /// <param name="presetIdentifier">The stable preset identifier.</param>
    void Apply(string presetIdentifier);
}
