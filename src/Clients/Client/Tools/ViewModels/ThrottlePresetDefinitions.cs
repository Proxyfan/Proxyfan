using Proxyfan.Domain.Throttling;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Builds the ordered, immutable list of built-in throttle preset
///     definitions exposed by the throttle tool window.
/// </summary>
public static class ThrottlePresetDefinitions
{
    /// <summary>
    ///     The stable identifier used for the "disable throttling" entry.
    /// </summary>
    public const string OffIdentifier = "Off";

    /// <summary>
    ///     The resource key used to resolve the localized "Off" label.
    /// </summary>
    public const string OffResourceKey = "Tools_Throttle_Preset_Off";

    /// <summary>
    ///     Builds the ordered list of built-in throttle preset definitions.
    /// </summary>
    /// <returns>The list of preset definitions in display order.</returns>
    public static IReadOnlyList<ThrottlePresetDefinition> Create()
    {
        var off = new ThrottlePresetDefinition(OffIdentifier, OffResourceKey, static () => null);
        var secondGen = new ThrottlePresetDefinition("2G", "Tools_Throttle_Preset_2G", ThrottleProfilePresets.SlowSecondGeneration);
        var thirdGen = new ThrottlePresetDefinition("3G", "Tools_Throttle_Preset_3G", ThrottleProfilePresets.ThirdGeneration);
        var fourthGen = new ThrottlePresetDefinition("4G", "Tools_Throttle_Preset_4G", ThrottleProfilePresets.FastFourthGeneration);
        var wireless = new ThrottlePresetDefinition("WiFi", "Tools_Throttle_Preset_WiFi", ThrottleProfilePresets.Wireless);
        var badNetwork = new ThrottlePresetDefinition("Bad Network", "Tools_Throttle_Preset_BadNetwork", ThrottleProfilePresets.BadNetwork);
        var completeLoss = new ThrottlePresetDefinition("100% Loss", "Tools_Throttle_Preset_CompleteLoss", ThrottleProfilePresets.CompleteLoss);
        return [off, secondGen, thirdGen, fourthGen, wireless, badNetwork, completeLoss];
    }
}
