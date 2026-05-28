using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic.Tabs;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Proxyfan.Client.Shell.ViewModels;

/// <summary>
///     Manages the workspace tab strip that sits above the traffic list. Each tab persists
///     its own filter query and selected flow. Switching tabs saves the current tab's
///     state to its domain object and restores the new tab's state into the shared
///     <see cref="TrafficListViewModel" />. The first tab created is always
///     <see cref="TrafficWorkspaceTabSet.DefaultFirstTabName" />.
/// </summary>
public sealed partial class TabHostViewModel : ObservableObject
{
    private const string NewTabNamePrefix = "Tab ";
    private readonly TrafficWorkspaceTabSet _tabSet;
    [ObservableProperty]
    private TabViewModel _activeTab;
    [ObservableProperty]
    private int _activeTabIndex;
    private bool _isApplyingTabState;

    /// <summary>
    ///     Gets the observable collection of tabs shown in the strip.
    /// </summary>
    public ObservableCollection<TabViewModel> Tabs { get; }

    /// <summary>
    ///     Gets the shared traffic list view model. Bound by the active tab's content area.
    /// </summary>
    public TrafficListViewModel TrafficList { get; }

    /// <summary>
    ///     Initializes a new <see cref="TabHostViewModel" /> with one initial tab.
    /// </summary>
    /// <param name="trafficList">The shared traffic list view model.</param>
    public TabHostViewModel(TrafficListViewModel trafficList)
    {
        TrafficList = trafficList;
        var tabSet = new TrafficWorkspaceTabSet();
        _tabSet = tabSet;

        var tabs = new ObservableCollection<TabViewModel>();
        Tabs = tabs;
        var snapshot = _tabSet.Snapshot();
        var initial = new TabViewModel(snapshot[0]);
        Tabs.Add(initial);
        _activeTab = initial;
        _activeTabIndex = 0;
        UpdateCanCloseFlags();

        TrafficList.PropertyChanged += OnTrafficListPropertyChanged;
    }

    /// <summary>
    ///     Activates the tab at <paramref name="index" /> when in range.
    /// </summary>
    /// <param name="index">The zero-based tab index.</param>
    public void ActivateAt(int index)
    {
        if (index < 0 || index >= Tabs.Count)
        {
            return;
        }

        ActiveTabIndex = index;
    }

    /// <summary>
    ///     Activates the next tab, wrapping around when at the end.
    /// </summary>
    public void ActivateNext()
    {
        if (Tabs.Count <= 1)
        {
            return;
        }

        var next = (ActiveTabIndex + 1) % Tabs.Count;
        ActiveTabIndex = next;
    }

    /// <summary>
    ///     Activates the previous tab, wrapping around when at the start.
    /// </summary>
    public void ActivatePrevious()
    {
        if (Tabs.Count <= 1)
        {
            return;
        }

        var previous = (ActiveTabIndex - 1 + Tabs.Count) % Tabs.Count;
        ActiveTabIndex = previous;
    }

    [RelayCommand]
    private void AddTab()
    {
        var name = NewTabNamePrefix + (Tabs.Count + 1).ToString(CultureInfo.InvariantCulture);
        SaveActiveTabState();
        var domainTab = new TrafficWorkspaceTab(name);
        _tabSet.Add(domainTab);
        var viewModel = new TabViewModel(domainTab);
        Tabs.Add(viewModel);
        UpdateCanCloseFlags();
        ActiveTabIndex = Tabs.Count - 1;
    }

    private void ApplyTabState(TabViewModel tab)
    {
        _isApplyingTabState = true;
        try
        {
            TrafficList.FilterText = tab.Source.FilterQuery;
            var selectedId = tab.Source.SelectedFlowId;
            if (selectedId is null)
            {
                TrafficList.SelectedFlow = null;
                return;
            }

            TrafficFlowViewModel? match = null;
            foreach (var flow in TrafficList.Flows)
            {
                if (flow.Source.Id == selectedId.Value)
                {
                    match = flow;
                    break;
                }
            }

            TrafficList.SelectedFlow = match;
        }
        finally
        {
            _isApplyingTabState = false;
        }
    }

    [RelayCommand]
    private void CloseActiveTab()
    {
        CloseTab(ActiveTab);
    }

    [RelayCommand]
    private void CloseTab(TabViewModel? tab)
    {
        if (tab is null)
        {
            return;
        }

        if (Tabs.Count <= 1)
        {
            return;
        }

        var index = Tabs.IndexOf(tab);
        if (index < 0)
        {
            return;
        }

        _tabSet.Close(index);
        Tabs.RemoveAt(index);
        if (ActiveTabIndex >= Tabs.Count)
        {
            ActiveTabIndex = Tabs.Count - 1;
        }
        else
        {
            ActiveTab = Tabs[ActiveTabIndex];
            ApplyTabState(ActiveTab);
        }

        UpdateCanCloseFlags();
    }

    partial void OnActiveTabIndexChanged(int value)
    {
        if (value < 0 || value >= Tabs.Count)
        {
            return;
        }

        var newTab = Tabs[value];
        if (!ReferenceEquals(ActiveTab, newTab))
        {
            SaveActiveTabState();
            ActiveTab = newTab;
            _tabSet.Activate(value);
            ApplyTabState(newTab);
        }
    }

    private void OnTrafficListPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (_isApplyingTabState)
        {
            return;
        }

        if (args.PropertyName == nameof(TrafficListViewModel.FilterText))
        {
            ActiveTab.Source.SetFilterQuery(TrafficList.FilterText);
            return;
        }

        if (args.PropertyName == nameof(TrafficListViewModel.SelectedFlow))
        {
            var selected = TrafficList.SelectedFlow;
            ActiveTab.Source.SetSelectedFlowId(selected?.Source.Id);
        }
    }

    private void SaveActiveTabState()
    {
        if (_isApplyingTabState)
        {
            return;
        }

        var current = ActiveTab;
        current.Source.SetFilterQuery(TrafficList.FilterText);
        var selected = TrafficList.SelectedFlow;
        current.Source.SetSelectedFlowId(selected?.Source.Id);
    }

    private void UpdateCanCloseFlags()
    {
        var canClose = Tabs.Count > 1;
        foreach (var tab in Tabs)
        {
            tab.IsCloseable = canClose;
        }
    }
}
