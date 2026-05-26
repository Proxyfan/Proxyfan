using Avalonia.Data;
using System;

namespace Proxyfan.Presentation.Localization;

/// <summary>
///     Creates a binding that resolves a localized string from the shared
///     <see cref="LocalizationService" /> and updates automatically on locale changes.
/// </summary>
public sealed class LocalizeExtension
{
    /// <summary>
    ///     Gets or sets the resource key to resolve.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    ///     Provides the localized binding or a fallback string when localization is unavailable.
    /// </summary>
    /// <param name="_">
    ///     The markup service provider. Not used by this implementation.
    /// </param>
    /// <returns>
    ///     A <see cref="Binding" /> targeting <see cref="LocalizationService" />,
    ///     or the key string itself when the service is not yet available.
    /// </returns>
    public object ProvideValue(IServiceProvider _)
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            return string.Empty;
        }

        var localizationKey = Key;
        if (ContainerLocator.Current?.GetService(typeof(LocalizationService)) is not LocalizationService service)
        {
            return localizationKey;
        }

        var binding = new Binding
        {
            Source = service,
            Path = $"[{localizationKey}]",
            Mode = BindingMode.OneWay,
        };
        return binding;
    }
}