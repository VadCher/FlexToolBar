using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents the root toolbar control hosting multiple tabs and managing group expansion behavior.
    /// Supports automated state serialization, factory resets, and lazy debounced auto-saving.
    /// </summary>
    public class ToolBar : TemplatedControl
    {
        private readonly FlexLayoutManager _coreLayoutManager = new();
        private readonly DispatcherTimer _autoSaveTimer;
        private Window? _parentWindow;
        private TabStrip? _tabStrip;
        private bool _xamlDefaultIsSingleExpand = false;
        private string _currentlyLoadedThemeName = "Default";
        private bool _isTabHeaderVisible;
        private readonly Dictionary<string, object> _themeRegistry = new();

        private ScrollViewer? _scrollViewer;
        private Button? _scrollLeftButton;
        private Button? _scrollRightButton;

        public static readonly AttachedProperty<string> ActiveThemeNameProperty =
            AvaloniaProperty.RegisterAttached<ToolBar, AvaloniaObject, string>("ActiveThemeName", "Default", inherits: true);

        public static string GetActiveThemeName(AvaloniaObject element) => element.GetValue(ActiveThemeNameProperty);
        public static void SetActiveThemeName(AvaloniaObject element, string value) => element.SetValue(ActiveThemeNameProperty, value);

        public static readonly StyledProperty<ObservableCollection<string>> AvailableThemesProperty =
            AvaloniaProperty.Register<ToolBar, ObservableCollection<string>>(
                nameof(AvailableThemes),
                new ObservableCollection<string>());

        public ObservableCollection<string> AvailableThemes
        {
            get => GetValue(AvailableThemesProperty);
            set => SetValue(AvailableThemesProperty, value);
        }

        public static readonly StyledProperty<global::Avalonia.Controls.Dock> PanelEdgeProperty =
            AvaloniaProperty.Register<ToolBar, global::Avalonia.Controls.Dock>(nameof(PanelEdge), global::Avalonia.Controls.Dock.Top);

        public global::Avalonia.Controls.Dock PanelEdge
        {
            get => GetValue(PanelEdgeProperty);
            set => SetValue(PanelEdgeProperty, value);
        }

        public static readonly StyledProperty<object> ScrollLeftContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(ScrollLeftContent), "◀");

        public object ScrollLeftContent
        {
            get => GetValue(ScrollLeftContentProperty);
            set => SetValue(ScrollLeftContentProperty, value);
        }

        public static readonly StyledProperty<object> ScrollRightContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(ScrollRightContent), "▶");

        public object ScrollRightContent
        {
            get => GetValue(ScrollRightContentProperty);
            set => SetValue(ScrollRightContentProperty, value);
        }

        public static readonly AttachedProperty<double> GroupSpacingProperty =
            AvaloniaProperty.RegisterAttached<ToolBar, AvaloniaObject, double>("GroupSpacing", 6.0, inherits: true);

        public static double GetGroupSpacing(AvaloniaObject element) => element.GetValue(GroupSpacingProperty);
        public static void SetGroupSpacing(AvaloniaObject element, double value) => element.SetValue(GroupSpacingProperty, value);

        public static readonly StyledProperty<bool> IsSingleExpandGroupProperty =
            AvaloniaProperty.Register<ToolBar, bool>(nameof(IsSingleExpandGroup), defaultValue: false);

        public static readonly StyledProperty<ObservableCollection<Tab>> TabsProperty =
            AvaloniaProperty.Register<ToolBar, ObservableCollection<Tab>>(nameof(Tabs), defaultValue: new ObservableCollection<Tab>());

        public static readonly StyledProperty<string?> AutoSaveIdProperty =
            AvaloniaProperty.Register<ToolBar, string?>(nameof(AutoSaveId), defaultValue: null);

        public static readonly StyledProperty<bool> RestoreSelectedTabProperty =
            AvaloniaProperty.Register<ToolBar, bool>(nameof(RestoreSelectedTab), defaultValue: false);

        public static readonly StyledProperty<TimeSpan> AutoSaveIntervalProperty =
            AvaloniaProperty.Register<ToolBar, TimeSpan>(nameof(AutoSaveInterval), defaultValue: TimeSpan.FromSeconds(5));

        private static readonly DirectProperty<ToolBar, bool> IsTabHeaderVisibleProperty =
            AvaloniaProperty.RegisterDirect<ToolBar, bool>(nameof(IsTabHeaderVisible), o => o.IsTabHeaderVisible);
        static ToolBar()
        {
            ToolBar.ActiveThemeNameProperty.Changed.AddClassHandler<ToolBar, string>(
                (sender, args) => sender.OnActiveThemeNameChanged(args.NewValue.Value));

            TabsProperty.Changed.AddClassHandler<ToolBar>((x, e) => x.OnTabsChanged(e));
        }

        /// <summary>
        /// Accessor token exposing the root cross-platform technical view model state registry.
        /// </summary>
        public FlexToolBarViewModel ViewModel { get; } = new();

        public ToolBar()
        {
            ResetLayoutCommand = new MiniRelayCommand(ResetToDefaultLayout);

            _autoSaveTimer = new DispatcherTimer(DispatcherPriority.Background);
            _autoSaveTimer.Tick += OnAutoSaveTimerTick;

            if (Tabs != null) Tabs.CollectionChanged += OnTabsCollectionChanged;
            UpdateTabHeaderVisibility();

            AvailableThemes = ToolBarThemeManager.AvailableThemes;

            // Native MVVM sync bridge: keeps the UI properties locked to follow the core model fields TwoWay
            this.Bind(IsSingleExpandGroupProperty, new global::Avalonia.Data.Binding(nameof(ViewModel.IsSingleExpandGroup)) { Source = ViewModel, Mode = global::Avalonia.Data.BindingMode.TwoWay });
            this.Bind(GroupSpacingProperty, new global::Avalonia.Data.Binding(nameof(ViewModel.GroupSpacing)) { Source = ViewModel, Mode = global::Avalonia.Data.BindingMode.TwoWay });
            this.Bind(ActiveThemeNameProperty, new global::Avalonia.Data.Binding(nameof(ViewModel.ActiveThemeName)) { Source = ViewModel, Mode = global::Avalonia.Data.BindingMode.TwoWay });

            // Reactive save trigger: starts the debounce window only when the core model tells us it is edited
            ViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.IsEdited))
                {
                    if (ViewModel.IsEdited)
                    {
                        RequestAutoSave();
                    }
                    else
                    {
                        _autoSaveTimer.Stop(); // Forcefully inhibit background execution when state flushes to false
                    }
                }
            };
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
        private void OnActiveThemeNameChanged(string selectedTheme)
        {
            if (string.IsNullOrEmpty(selectedTheme) || selectedTheme == _currentlyLoadedThemeName) return;

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ApplyThemeDirect(selectedTheme);
            }, global::Avalonia.Threading.DispatcherPriority.Background);
        }

        private void ApplyThemeDirect(string selectedTheme)
        {
            if (string.IsNullOrEmpty(selectedTheme) || selectedTheme == _currentlyLoadedThemeName) return;

            _currentlyLoadedThemeName = selectedTheme;

            // SUCCESSFUL PIPELINE: Mutating local Styles collection directly forces instantaneous tree invalidation
            this.Styles.Clear();

            if (selectedTheme == "Default") return;

            if (ToolBarThemeManager.TryGetThemeUri(selectedTheme, out var targetUri) && targetUri != null)
            {
                try
                {
                    // Safe cross-assembly runtime styles sheet injector token
                    var styleInclude = new global::Avalonia.Markup.Xaml.Styling.StyleInclude(targetUri) { Source = targetUri };
                    
                    this.Styles.Add(styleInclude);
                }
                catch (Exception) { }
            }
        }
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _tabStrip = e.NameScope.Find<TabStrip>("PART_TabSelectionStrip");
            if (_tabStrip != null)
            {
                _tabStrip.SelectionChanged += (s, args) =>
                {
                    if (RestoreSelectedTab && _tabStrip.SelectedItem is Tab activeUiTab)
                    {
                        ViewModel.SelectedTabId = Tab.GetTabId(activeUiTab);
                    }
                };
            }

            _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_TabScrollViewer");
            _scrollLeftButton = e.NameScope.Find<Button>("PART_ScrollLeftButton");
            _scrollRightButton = e.NameScope.Find<Button>("PART_ScrollRightButton");

            if (_scrollViewer != null) _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            if (_scrollLeftButton != null) _scrollLeftButton.Click += OnScrollLeftClick;
            if (_scrollRightButton != null) _scrollRightButton.Click += OnScrollRightClick;
        }

        private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null || _scrollLeftButton == null || _scrollRightButton == null) return;

            double currentX = _scrollViewer.Offset.X;
            double maxScrollableX = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;

            if (maxScrollableX <= 0)
            {
                _scrollLeftButton.IsVisible = false;
                _scrollRightButton.IsVisible = false;
                return;
            }

            _scrollLeftButton.IsVisible = currentX > 0.5;
            _scrollRightButton.IsVisible = currentX < maxScrollableX - 0.5;
        }

        private void OnScrollRightClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_scrollViewer == null) return;
            double maxScrollX = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;
            _scrollViewer.Offset = _scrollViewer.Offset.WithX(maxScrollX);
        }

        private void OnScrollLeftClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_scrollViewer == null) return;
            _scrollViewer.Offset = _scrollViewer.Offset.WithX(0);
        }
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _xamlDefaultIsSingleExpand = IsSingleExpandGroup;

            if (!string.IsNullOrWhiteSpace(AutoSaveId))
            {
                _parentWindow = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();
                if (_parentWindow != null) _parentWindow.Closing += OnParentWindowClosing;
            }
        }

        protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnLoaded(e);

            foreach (var uiTab in Tabs)
            {
                string tabId = Tab.GetTabId(uiTab);
                if (uiTab.Items == null) continue;

                foreach (var item in uiTab.Items)
                {
                    if (item is FlexGroup uiGroup)
                    {
                        uiGroup.GroupViewModel.TabId = tabId;
                        ViewModel.RegisterGroup(uiGroup.GroupId, uiGroup.GroupViewModel);
                    }
                }
            }

            RefreshLayout();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _autoSaveTimer.Stop();
            if (_parentWindow != null) _parentWindow.Closing -= OnParentWindowClosing;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnParentWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _autoSaveTimer.Stop();
            TriggerLayoutSaveSequence();
        }

        public void RequestAutoSave()
        {
            if (AutoSaveInterval == TimeSpan.Zero || string.IsNullOrWhiteSpace(AutoSaveId)) return;

            _autoSaveTimer.Stop();
            _autoSaveTimer.Interval = AutoSaveInterval;
            _autoSaveTimer.Start();
        }

        private void OnAutoSaveTimerTick(object? sender, EventArgs e)
        {
            _autoSaveTimer.Stop();
            TriggerLayoutSaveSequence();
        }

        private void TriggerLayoutSaveSequence()
        {
            if (string.IsNullOrWhiteSpace(AutoSaveId)) return;
            _coreLayoutManager.SaveLayout(ViewModel, AutoSaveId);
        }

        public void RefreshLayout()
        {
            if (string.IsNullOrWhiteSpace(AutoSaveId)) return;

            bool isLoaded = _coreLayoutManager.LoadLayout(ViewModel, AutoSaveId);

            if (isLoaded)
            {
                if (RestoreSelectedTab && !string.IsNullOrWhiteSpace(ViewModel.SelectedTabId) && _tabStrip != null)
                {
                    var targetUiTab = Tabs.FirstOrDefault(t => Tab.GetTabId(t) == ViewModel.SelectedTabId);
                    if (targetUiTab != null)
                    {
                        _tabStrip.SelectedItem = targetUiTab;
                    }
                }
            }
            else
            {
                TriggerLayoutSaveSequence();
            }
        }
        public void ApplyLayoutJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            _coreLayoutManager.LoadLayout(ViewModel, json);

            if (RestoreSelectedTab && !string.IsNullOrWhiteSpace(ViewModel.SelectedTabId) && _tabStrip != null)
            {
                var targetUiTab = Tabs.FirstOrDefault(t => Tab.GetTabId(t) == ViewModel.SelectedTabId);
                if (targetUiTab != null)
                {
                    _tabStrip.SelectedItem = targetUiTab;
                }
            }

            ApplyThemeDirect(ViewModel.ActiveThemeName);
        }

        public void ResetToDefaultLayout()
        {
            if (!string.IsNullOrWhiteSpace(AutoSaveId))
            {
                _coreLayoutManager.DeleteLayout(AutoSaveId);
            }

            this.ClearValue(GroupSpacingProperty);
            this.ClearValue(IsSingleExpandGroupProperty);
            this.ClearValue(ActiveThemeNameProperty);

            ApplyThemeDirect("Default");

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
            if (e.OldValue is ObservableCollection<Tab> oldCollection) oldCollection.CollectionChanged -= OnTabsCollectionChanged;
            if (e.NewValue is ObservableCollection<Tab> newCollection) newCollection.CollectionChanged += OnTabsCollectionChanged;
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
    }

    internal class MiniRelayCommand : ICommand
    {
        private readonly Action _execute;
        public MiniRelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
