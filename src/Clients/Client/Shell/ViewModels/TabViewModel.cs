using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.Traffic.Tabs;
using System;

namespace Proxyfan.Client.Shell.ViewModels;

/// <summary>
///     View model that mirrors a single <see cref="TrafficWorkspaceTab" /> for binding in
///     the shell's tab strip. Exposes the user-visible name as an observable property and
///     forwards renames back into the underlying domain object.
/// </summary>
public sealed partial class TabViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCloseable;
    [ObservableProperty]
    private string _name;

    /// <summary>
    ///     Gets the stable identifier of the underlying tab.
    /// </summary>
    public Guid Id => Source.Id;

    /// <summary>
    ///     Gets the wrapped domain tab.
    /// </summary>
    public TrafficWorkspaceTab Source { get; }

    /// <summary>
    ///     Initializes a new <see cref="TabViewModel" /> for the supplied domain tab.
    /// </summary>
    /// <param name="source">The domain tab being wrapped.</param>
    public TabViewModel(TrafficWorkspaceTab source)
    {
        Source = source;
        _name = source.Name;
        _isCloseable = true;
        Source.Changed += OnSourceChanged;
    }

    /// <summary>
    ///     Renames the underlying tab. Whitespace and null are ignored.
    /// </summary>
    /// <param name="name">The proposed new name.</param>
    public void Rename(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Source.SetName(name);
    }

    private void OnSourceChanged(TrafficWorkspaceTab tab)
    {
        Name = tab.Name;
    }
}
