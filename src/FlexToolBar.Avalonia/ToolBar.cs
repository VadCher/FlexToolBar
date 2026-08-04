using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia;

public class ToolBar : TemplatedControl
{
    private readonly FlexLayoutManager _coreLayoutManager = new();
    private Window? _parentWindow;
    private TabStrip? _tabStrip; // Reference to apply loaded index directly
    private bool _isInitialBoot = true;

    public static readonly StyledProperty<bool> IsSingleExpandGroupProperty =
        AvaloniaProperty.Register<ToolBar, bool>(nameof(IsSingleExpandGroup), defaultValue: false);

    public static readonly StyledProperty<ObservableCollection<Tab>> TabsProperty =
        AvaloniaProperty.Register<ToolBar, ObservableCollection<Tab>>(
            nameof(Tabs),
            defaultValue: new ObservableCollection<Tab>());

    public static readonly StyledProperty<string?> AutoSaveIdProperty =
        AvaloniaProperty.Register<ToolBar, string?>(nameof(AutoSaveId), defaultValue: null);

    // NEW PROPERTY: Controls whether the control restores the last active tab workspace
    public static readonly StyledProperty<bool> RestoreSelectedTabProperty =
        AvaloniaProperty.Register<ToolBar, bool>(nameof(RestoreSelectedTab), defaultValue: false);

    private static readonly DirectProperty<ToolBar, bool> IsTabHeaderVisibleProperty =
        AvaloniaProperty.RegisterDirect<ToolBar, bool>(
            nameof(IsTabHeaderVisible),
            o => o.IsTabHeaderVisible);

    private bool _isTabHeaderVisible;

    static ToolBar()
    {
        TabsProperty.Changed.AddClassHandler<ToolBar>((x, e) => x.OnTabsChanged(e));
        
        IsSingleExpandGroupProperty.Changed.AddClassHandler<ToolBar>((toolBar, e) =>
        {
            if (e.NewValue is true) toolBar.EnforceSingleExpandLayout();
        });

        FlexGroup.IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((group, e) =>
        {
            if (e.NewValue is true)
            {
                var toolBar = group.GetLogicalAncestors().OfType<ToolBar>().FirstOrDefault();
                if (toolBar != null && toolBar.IsSingleExpandGroup)
                {
                    toolBar.CollapseSiblingGroups(group);
                }
            }
        });
    }

    public ToolBar()
    {
        ResetLayoutCommand = new MiniRelayCommand(ResetToDefaultLayout);
        if (Tabs != null) Tabs.CollectionChanged += OnTabsCollectionChanged;
        UpdateTabHeaderVisibility();
    }

    public bool IsSingleExpandGroup
    {
        get => GetValue(IsSingleExpandGroupProperty);
        set => SetValue(IsSingleExpandGroupProperty, value);
    }

    public ObservableCollection<Tab> Tabs
    {
        get => GetValue(TabsProperty);
        set => SetValue(TabsProperty, value);
    }

    public string? AutoSaveId
    {
        get => GetValue(AutoSaveIdProperty);
        set => SetValue(AutoSaveIdProperty, value);
    }

    public bool RestoreSelectedTab
    {
        get => GetValue(RestoreSelectedTabProperty);
        set => SetValue(RestoreSelectedTabProperty, value);
    }

    public bool IsTabHeaderVisible
    {
        get => _isTabHeaderVisible;
        private set => SetAndRaise(IsTabHeaderVisibleProperty, ref _isTabHeaderVisible, value);
    }

    public ICommand ResetLayoutCommand { get; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        // Capture the internal TabStrip template part to alter active selections natively
        _tabStrip = e.NameScope.Find<TabStrip>("PART_TabSelectionStrip");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!string.IsNullOrWhiteSpace(AutoSaveId))
        {
            _parentWindow = System.Linq.Enumerable.OfType<Window>(this.GetVisualAncestors()).FirstOrDefault();
            if (_parentWindow != null) _parentWindow.Closing += OnParentWindowClosing;
        }
    }

    protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RefreshLayout();
        _isInitialBoot = false;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_parentWindow != null) _parentWindow.Closing -= OnParentWindowClosing;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnParentWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AutoSaveId)) return;
        try
        {
            string json = GetLayoutJson();
            string filePath = GetTargetLayoutFilePath();
            File.WriteAllText(filePath, json);
        }
        catch { }
    }

    private string GetTargetLayoutFilePath()
    {
        string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(rootPath, $"{AutoSaveId}.json");
    }

    public void RefreshLayout()
    {
        if (string.IsNullOrWhiteSpace(AutoSaveId)) return;
        try
        {
            string filePath = GetTargetLayoutFilePath();
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                ApplyLayoutJson(json);
            }
        }
        catch { }
    }
    public string GetLayoutJson()
    {
        // 1. Resolve currently active TabId dynamically from template part
        string activeTabId = string.Empty;
        if (_tabStrip != null && _tabStrip.SelectedItem is Tab selectedUiTab)
        {
            activeTabId = Tab.GetTabId(selectedUiTab);
        }

        var coreModel = new FlexToolBarViewModel
        {
            SelectedTabId = activeTabId, // Capture active workspace partition
            IsSingleExpandGroup = IsSingleExpandGroup
        };

        foreach (var uiTab in Tabs)
        {
            string tabId = Tab.GetTabId(uiTab);
            string tabHeader = uiTab.Header?.ToString() ?? string.Empty;

            var coreTab = new FlexTabViewModel(tabId, tabHeader);

            if (uiTab.Items != null)
            {
                foreach (var item in uiTab.Items)
                {
                    if (item is FlexGroup uiGroup)
                    {
                        string groupId = FlexGroup.GetGroupId(uiGroup);
                        string groupHeader = uiGroup.Header;

                        coreTab.Groups.Add(new FlexGroupViewModel(groupId, groupHeader)
                        {
                            IsExpanded = uiGroup.IsExpanded,
                            IsPinned = uiGroup.IsPinned
                        });
                    }
                }
            }

            coreModel.Tabs.Add(coreTab);
        }

        return _coreLayoutManager.SaveLayout(coreModel);
    }

    public void ApplyLayoutJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var state = System.Text.Json.JsonSerializer.Deserialize<FlexToolbarState>(json, options);
            if (state == null) return;

            IsSingleExpandGroup = state.IsSingleExpandMode;

            // 2. RESTORE WORKSPACE FOCUS: Match and force the selected tab index natively if allowed
            if (RestoreSelectedTab && !string.IsNullOrWhiteSpace(state.SelectedTabId) && _tabStrip != null)
            {
                var targetUiTab = Tabs.FirstOrDefault(t => Tab.GetTabId(t) == state.SelectedTabId);
                if (targetUiTab != null)
                {
                    _tabStrip.SelectedItem = targetUiTab;
                }
            }

            if (state.Groups == null || !state.Groups.Any()) return;

            var loadedGroups = state.Groups
                .Where(g => !string.IsNullOrEmpty(g.GroupId))
                .ToDictionary(g => g.GroupId, g => g);

            foreach (var uiTab in Tabs)
            {
                if (uiTab.Items != null)
                {
                    foreach (var item in uiTab.Items)
                    {
                        if (item is FlexGroup uiGroup)
                        {
                            string groupId = FlexGroup.GetGroupId(uiGroup);
                            if (loadedGroups.TryGetValue(groupId, out var savedState))
                            {
                                uiGroup.IsExpanded = savedState.IsExpanded;
                                uiGroup.IsPinned = savedState.IsPinned;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception) { }
    }

    public void ResetToDefaultLayout()
    {
        try
        {
            string filePath = GetTargetLayoutFilePath();
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch { }

        IsSingleExpandGroup = false;

        // Reset workspace focus back to the very first tab on factory reset execution
        if (_tabStrip != null && Tabs.Any())
        {
            _tabStrip.SelectedIndex = 0;
        }

        foreach (var uiTab in Tabs)
        {
            if (uiTab.Items != null)
            {
                foreach (var item in uiTab.Items)
                {
                    if (item is FlexGroup uiGroup)
                    {
                        uiGroup.IsExpanded = uiGroup.XamlDefaultIsExpanded;
                        uiGroup.IsPinned = uiGroup.XamlDefaultIsPinned;
                    }
                }
            }
        }
    }

    private void OnTabsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is ObservableCollection<Tab> oldCollection)
        {
            oldCollection.CollectionChanged -= OnTabsCollectionChanged;
        }
        if (e.NewValue is ObservableCollection<Tab> newCollection)
        {
            newCollection.CollectionChanged += OnTabsCollectionChanged;
        }
        UpdateTabHeaderVisibility();
    }

    private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is ILogical logicalItem) LogicalChildren.Remove(logicalItem);
            }
        }
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is ILogical logicalItem) LogicalChildren.Add(logicalItem);
            }
        }
        UpdateTabHeaderVisibility();
    }

    private void UpdateTabHeaderVisibility()
    {
        IsTabHeaderVisible = Tabs != null && Tabs.Count > 1;
    }

    private void EnforceSingleExpandLayout()
    {
        if (Tabs == null) return;
        foreach (var tab in Tabs)
        {
            bool foundFirstDynamic = false;
            if (tab.Items != null)
            {
                foreach (var item in tab.Items)
                {
                    if (item is FlexGroup group && !group.IsPinned)
                    {
                        if (!foundFirstDynamic && group.IsExpanded) foundFirstDynamic = true;
                        else group.IsExpanded = false;
                    }
                }
            }
        }
    }

    private void CollapseSiblingGroups(FlexGroup activeGroup)
    {
        var targetTab = activeGroup.GetLogicalAncestors().OfType<Tab>().FirstOrDefault();
        if (targetTab == null) return;

        if (targetTab.Items != null)
        {
            foreach (var item in targetTab.Items)
            {
                if (item is FlexGroup sibling && sibling != activeGroup && !sibling.IsPinned)
                {
                    sibling.IsExpanded = false;
                }
            }
        }
    }

    private class MiniRelayCommand : ICommand
    {
        private readonly Action _execute;
        public MiniRelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
