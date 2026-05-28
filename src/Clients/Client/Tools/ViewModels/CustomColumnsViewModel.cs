using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Traffic.Columns;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Custom Columns tool window. Binds to
///     <see cref="CustomColumnRegistry" />, exposing the registered columns as an
///     observable collection plus editor fields and commands for adding and removing
///     custom columns.
/// </summary>
public sealed partial class CustomColumnsViewModel : ObservableObject, IDisposable
{
    private readonly CustomColumnRegistry _registry;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _newColumnDisplayName;
    [ObservableProperty]
    private string _newColumnHeaderKey;
    [ObservableProperty]
    private CustomColumnSource _newColumnSource;

    /// <summary>
    ///     Gets the observable collection of columns currently registered.
    /// </summary>
    public ObservableCollection<CustomColumnEntryViewModel> Columns { get; }

    /// <summary>
    ///     Initializes a new <see cref="CustomColumnsViewModel" /> bound to the supplied registry.
    /// </summary>
    /// <param name="registry">The custom column registry to expose.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public CustomColumnsViewModel(CustomColumnRegistry registry, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _registry = registry;
        _userInterfaceScheduler = userInterfaceScheduler;
        _newColumnDisplayName = string.Empty;
        _newColumnHeaderKey = string.Empty;
        _newColumnSource = CustomColumnSource.Request;
        Columns = [];
        _registry.Changed += OnRegistryChanged;
        ReloadColumns();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registry.Changed -= OnRegistryChanged;
    }

    [RelayCommand]
    private void AddColumn()
    {
        var displayName = NewColumnDisplayName;
        var headerKey = NewColumnHeaderKey;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(headerKey))
        {
            return;
        }

        var definition = new CustomColumnDefinition
        {
            DisplayName = displayName.Trim(),
            HeaderKey = headerKey.Trim(),
            Id = Guid.NewGuid(),
            Source = NewColumnSource,
        };
        _registry.Add(definition);
        NewColumnDisplayName = string.Empty;
        NewColumnHeaderKey = string.Empty;
    }

    private void OnRegistryChanged(CustomColumnRegistry sender)
    {
        _userInterfaceScheduler.Post(ReloadColumns);
    }

    private void ReloadColumns()
    {
        Columns.Clear();
        foreach (var column in _registry.Snapshot())
        {
            var viewModel = new CustomColumnEntryViewModel(column);
            Columns.Add(viewModel);
        }
    }

    [RelayCommand]
    private void RemoveColumn(CustomColumnEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        _registry.Remove(entry.Definition.Id);
    }
}
