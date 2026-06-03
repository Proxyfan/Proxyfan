using Proxyfan.Presentation.Localization;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Static helper that maps the stable English preset identifiers used by
///     <see cref="Proxyfan.Domain.Throttling.ThrottleProfile.Name" /> and by the
///     <see cref="Proxyfan.Domain.Throttling.ThrottleProfilePresets" /> library to
///     localized display labels sourced from the client Resources table. The
///     identifiers themselves remain in English so external mutations to the shared
///     <see cref="Proxyfan.Domain.Throttling.MutableThrottleProfile" /> can be
///     matched against the picker regardless of the active UI culture.
/// </summary>
public static class ThrottlePresetLabels
{
    /// <summary>
    ///     Stable identifier for the "no throttling" sentinel preset. Does not
    ///     correspond to a <see cref="Proxyfan.Domain.Throttling.ThrottleProfile" />.
    /// </summary>
    public const string OffPresetId = "Off";

    /// <summary>
    ///     Resource key used to look up the localized label for the "off" preset.
    /// </summary>
    public const string OffResourceKey = "Tools_Throttle_Preset_Off";

    /// <summary>
    ///     Returns the localized label for the supplied preset identifier by
    ///     resolving its resource key through the supplied
    ///     <see cref="LocalizationService" />. Unknown identifiers pass through
    ///     unchanged so externally-supplied profile names remain visible.
    /// </summary>
    /// <param name="presetId">The stable identifier of the preset.</param>
    /// <param name="localization">The localization service used to resolve the label.</param>
    /// <returns>The localized label text, or <paramref name="presetId" /> when unknown.</returns>
    public static string GetLabel(string presetId, LocalizationService localization)
    {
        var key = GetResourceKey(presetId);
        if (key is null)
        {
            return presetId;
        }

        return localization[key];
    }

    /// <summary>
    ///     Returns the resource key for the supplied preset identifier, or
    ///     <see langword="null" /> when the identifier is not one of the built-in
    ///     presets.
    /// </summary>
    /// <param name="presetId">The stable identifier of the preset.</param>
    /// <returns>The resource key, or <see langword="null" /> when unknown.</returns>
    public static string? GetResourceKey(string presetId)
    {
        if (presetId == OffPresetId)
        {
            return OffResourceKey;
        }

        if (presetId == "2G")
        {
            return "Tools_Throttle_Preset_2G";
        }

        if (presetId == "3G")
        {
            return "Tools_Throttle_Preset_3G";
        }

        if (presetId == "4G")
        {
            return "Tools_Throttle_Preset_4G";
        }

        if (presetId == "WiFi")
        {
            return "Tools_Throttle_Preset_WiFi";
        }

        if (presetId == "Bad Network")
        {
            return "Tools_Throttle_Preset_BadNetwork";
        }

        if (presetId == "100% Loss")
        {
            return "Tools_Throttle_Preset_CompleteLoss";
        }

        return null;
    }
}
