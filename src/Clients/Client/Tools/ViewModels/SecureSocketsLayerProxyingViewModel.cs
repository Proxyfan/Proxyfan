using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Certificates;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the secure-sockets-layer proxying tool window. Lets the user inspect
///     and edit the include/exclude host patterns and toggle TLS interception on or off.
/// </summary>
public sealed partial class SecureSocketsLayerProxyingViewModel : ObservableObject, IDisposable
{
    private readonly ServerNameIndicationProxyingList _list;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private string _newExcludedPattern;
    [ObservableProperty]
    private string _newIncludedPattern;
    [ObservableProperty]
    private string? _selectedExcludedPattern;
    [ObservableProperty]
    private string? _selectedIncludedPattern;

    /// <summary>
    ///     Gets the excluded host name patterns currently configured.
    /// </summary>
    public ObservableCollection<string> ExcludedPatterns { get; }

    /// <summary>
    ///     Gets the included host name patterns currently configured.
    /// </summary>
    public ObservableCollection<string> IncludedPatterns { get; }

    /// <summary>
    ///     Initializes a new <see cref="SecureSocketsLayerProxyingViewModel" /> bound to the supplied list.
    /// </summary>
    /// <param name="list">The proxying list to observe and mutate.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public SecureSocketsLayerProxyingViewModel(ServerNameIndicationProxyingList list, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _list = list;
        _userInterfaceScheduler = userInterfaceScheduler;
        _isEnabled = list.IsEnabled;
        _newIncludedPattern = string.Empty;
        _newExcludedPattern = string.Empty;
        IncludedPatterns = [];
        ExcludedPatterns = [];
        RefreshPatterns();
        _list.Changed += OnListChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _list.Changed -= OnListChanged;
    }

    [RelayCommand]
    private void AddExcludedPattern()
    {
        var pattern = NewExcludedPattern;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        _list.AddExcludedPattern(pattern.Trim());
        NewExcludedPattern = string.Empty;
    }

    [RelayCommand]
    private void AddIncludedPattern()
    {
        var pattern = NewIncludedPattern;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        _list.AddIncludedPattern(pattern.Trim());
        NewIncludedPattern = string.Empty;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (value && !_list.IsEnabled)
        {
            _list.Enable();
        }
        else if (!value && _list.IsEnabled)
        {
            _list.Disable();
        }
    }

    private void OnListChanged(ServerNameIndicationProxyingList sender)
    {
        _userInterfaceScheduler.Post(() =>
        {
            if (IsEnabled != _list.IsEnabled)
            {
                IsEnabled = _list.IsEnabled;
            }

            RefreshPatterns();
        });
    }

    private void RefreshPatterns()
    {
        IncludedPatterns.Clear();
        foreach (var pattern in _list.IncludedPatterns)
        {
            IncludedPatterns.Add(pattern);
        }

        ExcludedPatterns.Clear();
        foreach (var pattern in _list.ExcludedPatterns)
        {
            ExcludedPatterns.Add(pattern);
        }
    }

    [RelayCommand]
    private void RemoveExcludedPattern()
    {
        var pattern = SelectedExcludedPattern;
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }

        _list.RemoveExcludedPattern(pattern);
    }

    [RelayCommand]
    private void RemoveIncludedPattern()
    {
        var pattern = SelectedIncludedPattern;
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }

        _list.RemoveIncludedPattern(pattern);
    }
}
