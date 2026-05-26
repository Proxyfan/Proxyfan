using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Proxyfan.Presentation.Localization;

/// <summary>
///     Provides localized strings and tracks the active UI culture.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    /// <summary>
    ///     Occurs when a localized value or the active culture changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    private const string IndexerPropertyName = "Item[]";
    private readonly List<ResourceManager> _managers;
    private CultureInfo _currentCulture;

    /// <summary>
    ///     Gets or sets the currently active UI culture.
    /// </summary>
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Name == value.Name)
            {
                return;
            }

            _currentCulture = value;
            CultureInfo.CurrentUICulture = value;
            OnPropertyChanged(nameof(CurrentCulture));
            OnPropertyChanged(IndexerPropertyName);
        }
    }

    /// <summary>
    ///     Gets the localized string for the specified resource key.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    public string this[string key] => GetString(key);

    /// <summary>
    ///     Initializes a new instance of the <see cref="LocalizationService" /> class.
    /// </summary>
    /// <param name="initialCulture">The initial UI culture.</param>
    public LocalizationService(CultureInfo initialCulture)
    {
        var managers = new List<ResourceManager>();
        _managers = managers;
        _currentCulture = initialCulture;
        CultureInfo.CurrentUICulture = initialCulture;
    }

    /// <summary>
    ///     Registers a resource manager that provides localized strings.
    /// </summary>
    /// <param name="manager">The resource manager to register.</param>
    public void RegisterManager(ResourceManager manager)
    {
        _managers.Add(manager);
    }

    private string GetString(string key)
    {
        foreach (var manager in _managers)
        {
            var localizedValue = manager.GetString(key, _currentCulture);
            if (!string.IsNullOrEmpty(localizedValue))
            {
                return localizedValue;
            }
        }

        return key;
    }

    private void OnPropertyChanged(string propertyName)
    {
        var propertyChangedEventArgs = new PropertyChangedEventArgs(propertyName);
        PropertyChanged?.Invoke(this, propertyChangedEventArgs);
    }
}