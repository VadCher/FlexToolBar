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
using Avalonia.Threading; // Required for native DispatcherTimer execution
using Avalonia.VisualTree;
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia;

/// <summary>
/// Represents the root toolbar control hosting multiple tabs and managing group expansion behavior.
/// Supports automated state serialization, factory resets, and lazy debounced auto-saving.
/// </summary>
public class ToolBar : TemplatedControl
{
    private readonly FlexLayoutManager _coreLayoutManager = new();
    private readonly DispatcherTimer _autoSaveTimer; // Lazy debounce background drive
    private Window? _parentWindow;
    private TabStrip? _tabStrip;
    private bool _isInitialBoot = true;
    private bool _xamlDefaultIsSingleExpand = false;

    public static readonly StyledProperty<bool> IsSingleExpandGroupProperty =
        AvaloniaProperty.Register<ToolBar, bool>(nameof(IsSingleExpandGroup), defaultValue: false);

    public static readonly StyledProperty<ObservableCollection<Tab>> TabsProperty =
        AvaloniaProperty.Register<ToolBar, ObservableCollection<Tab>>(
            nameof(Tabs),
            defaultValue: new ObservableCollection<Tab>());

    public static readonly StyledProperty<string?> AutoSaveIdProperty =
        AvaloniaProperty.Register<ToolBar, string?>(nameof(AutoSaveId), defaultValue: null);

    public static readonly StyledProperty<bool> RestoreSelectedTabProperty =
        AvaloniaProperty.Register<ToolBar, bool>(nameof(RestoreSelectedTab), defaultValue: false);

    // NEW STYLED PROPERTY: Controls the lazy background auto-save debounce delay window
    public static readonly StyledProperty<TimeSpan> AutoSaveIntervalProperty =
        AvaloniaProperty.Register<ToolBar, TimeSpan>(
            nameof(AutoSaveInterval), 
            defaultValue: TimeSpan.FromSeconds(5)); // Golden standard 5-second default window

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
            toolBar.RequestAutoSave(); // Request save when single expand mode shifts
        });

        // Global interceptor for ANY group expansion property mutations within the system
        FlexGroup.IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((group, e) =>
        {
            var toolBar = group.GetLogicalAncestors().OfType<ToolBar>().FirstOrDefault();
            if (toolBar != null)
            {
                if (e.NewValue is true && toolBar.IsSingleExpandGroup)
                {
                    toolBar.CollapseSiblingGroups(group);
                }
                toolBar.RequestAutoSave(); // Natively trigger the lazy save cooldown
            }
        });

        // Global interceptor for ANY group pin status property mutations within the system
        FlexGroup.IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((group, e) =>
        {
            var toolBar = group.GetLogicalAncestors().OfType<ToolBar>().FirstOrDefault();
            toolBar?.RequestAutoSave(); // Natively trigger the lazy save cooldown
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolBar"/> class.
    /// </summary>
    public ToolBar()
    {
        ResetLayoutCommand = new MiniRelayCommand(ResetToDefaultLayout);

        // 1. Initialize the native DispatcherTimer directly synchronized with the UI thread
        _autoSaveTimer = new DispatcherTimer(DispatcherPriority.Background);
        _autoSaveTimer.Tick += OnAutoSaveTimerTick;

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

    public TimeSpan AutoSaveInterval
    {
        get => GetValue(AutoSaveIntervalProperty);
        set => SetValue(AutoSaveIntervalProperty, value);
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
        _tabStrip = e.NameScope.Find<TabStrip>("PART_TabSelectionStrip");
        
        // Listen to tab selection changes to trigger auto-save if tab tracking is requested
        if (_tabStrip != null)
        {
            _tabStrip.SelectionChanged += (s, args) =>
            {
                if (RestoreSelectedTab) RequestAutoSave();
            };
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _xamlDefaultIsSingleExpand = IsSingleExpandGroup;

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
        _autoSaveTimer.Stop(); // Prevent background ticks if the control detaches from live UI
        if (_parentWindow != null) _parentWindow.Closing -= OnParentWindowClosing;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnParentWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _autoSaveTimer.Stop(); // Inhibit timer to bypass race conditions on app exit
        TriggerLayoutSaveSequence();
    }
    /// <summary>
    /// Requests a lazy background auto-save operation. 
    /// Resets the countdown timer to achieve a clean debouncing effect.
    /// </summary>
    public void RequestAutoSave()
    {
        // 1. If the developer explicitly disabled the timer or AutoSaveId is blank, skip execution
        if (AutoSaveInterval == TimeSpan.Zero || string.IsNullOrWhiteSpace(AutoSaveId) || _isInitialBoot)
        {
            return;
        }

        // 2. THE DEBOUNCE MAGIC: Stop and restart the timer to push the save window forward
        _autoSaveTimer.Stop();
        _autoSaveTimer.Interval = AutoSaveInterval;
        _autoSaveTimer.Start();
    }

    private void OnAutoSaveTimerTick(object? sender, EventArgs e)
    {
        // 3. Stop the timer immediately so it doesn't loop, and flush data to disk
        _autoSaveTimer.Stop();
        TriggerLayoutSaveSequence();
    }

    private void TriggerLayoutSaveSequence()
    {
        if (string.IsNullOrWhiteSpace(AutoSaveId)) return;

        try
        {
            string json = GetLayoutJson();
            string filePath = GetTargetLayoutFilePath();
            File.WriteAllText(filePath, json);
        }
        catch { /* Resilient file system blocks protection */ }
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
        string activeTabId = string.Empty;
        if (_tabStrip != null && _tabStrip.SelectedItem is Tab selectedUiTab)
        {
            activeTabId = Tab.GetTabId(selectedUiTab);
        }

        var coreModel = new FlexToolBarViewModel
        {
            SelectedTabId = activeTabId,
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

        IsSingleExpandGroup = _xamlDefaultIsSingleExpand;

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
