using System;
using System.Collections;
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
        private Window? _parentWindow;
        private TabStrip? _tabStrip;
        private string _currentlyLoadedThemeName = "Default";

        private ScrollViewer? _scrollViewer;
        private Button? _scrollLeftButton;
        private Button? _scrollRightButton;

        // Static Infrastructure Tokens for unified background debounced saving loops
        private static DispatcherTimer? _globalSaveTimer;
        private static bool _isSaveBridgeSubscribed;

        public static readonly AttachedProperty<string> ActiveThemeNameProperty =
            AvaloniaProperty.RegisterAttached<ToolBar, AvaloniaObject, string>("ActiveThemeName", "Default", inherits: true);

        public static string GetActiveThemeName(AvaloniaObject element) => element.GetValue(ActiveThemeNameProperty);
        public static void SetActiveThemeName(AvaloniaObject element, string value) => element.SetValue(ActiveThemeNameProperty, value);

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

        // Parallel Data Bridge Properties (Clean Styled Properties, free for custom developer bindings)
        public static readonly AttachedProperty<double> GroupSpacingProperty =
            AvaloniaProperty.RegisterAttached<ToolBar, AvaloniaObject, double>("GroupSpacing", 6.0, inherits: true);

        public static double GetGroupSpacing(AvaloniaObject element) => element.GetValue(GroupSpacingProperty);
        public static void SetGroupSpacing(AvaloniaObject element, double value) => element.SetValue(GroupSpacingProperty, value);

        public static readonly StyledProperty<bool> IsSingleExpandGroupProperty =
            AvaloniaProperty.Register<ToolBar, bool>(nameof(IsSingleExpandGroup), defaultValue: false);

        public bool IsSingleExpandGroup
        {
            get => GetValue(IsSingleExpandGroupProperty);
            set => SetValue(IsSingleExpandGroupProperty, value);
        }

        public static readonly StyledProperty<bool> TabStripVisibleProperty =
            AvaloniaProperty.Register<ToolBar, bool>(nameof(TabStripVisible), defaultValue: true);

        public bool TabStripVisible
        {
            get => GetValue(TabStripVisibleProperty);
            set => SetValue(TabStripVisibleProperty, value);
        }
        public static readonly StyledProperty<bool> TabsVisibleProperty =
            AvaloniaProperty.Register<ToolBar, bool>(nameof(TabsVisible), defaultValue: true);

        public bool TabsVisible
        {
            get => GetValue(TabsVisibleProperty);
            set => SetValue(TabsVisibleProperty, value);
        }

        public static readonly StyledProperty<ObservableCollection<Tab>> TabsProperty =
            AvaloniaProperty.Register<ToolBar, ObservableCollection<Tab>>(nameof(Tabs));

        public ObservableCollection<Tab> Tabs
        {
            get => GetValue(TabsProperty);
            set => SetValue(TabsProperty, value);
        }

        public static readonly StyledProperty<string?> ToolBarIdProperty =
            AvaloniaProperty.Register<ToolBar, string?>(nameof(ToolBarId), defaultValue: null);

        public string? ToolBarId
        {
            get => GetValue(ToolBarIdProperty);
            set => SetValue(ToolBarIdProperty, value);
        }

        public static readonly StyledProperty<bool> RestoreSelectedTabProperty =
            AvaloniaProperty.Register<ToolBar, bool>(nameof(RestoreSelectedTab), defaultValue: false);

        public bool RestoreSelectedTab
        {
            get => GetValue(RestoreSelectedTabProperty);
            set => SetValue(RestoreSelectedTabProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> AutoSaveIntervalProperty =
            AvaloniaProperty.Register<ToolBar, TimeSpan>(nameof(AutoSaveInterval), defaultValue: TimeSpan.FromSeconds(5));

        public TimeSpan AutoSaveInterval
        {
            get => GetValue(AutoSaveIntervalProperty);
            set => SetValue(AutoSaveIntervalProperty, value);
        }

        static ToolBar()
        {
            TabsProperty.Changed.AddClassHandler<ToolBar>((x, e) => x.OnTabsChanged(e));

            // UI -> Core Direction (Local geometric properties remain in ViewModel)
            IsSingleExpandGroupProperty.Changed.AddClassHandler<ToolBar>((x, e) => { if (x.ViewModel is not null) x.ViewModel.IsSingleExpandGroup = e.GetNewValue<bool>(); });
            TabStripVisibleProperty.Changed.AddClassHandler<ToolBar>((x, e) => { if (x.ViewModel is not null) x.ViewModel.TabStripVisible = e.GetNewValue<bool>(); });
            TabsVisibleProperty.Changed.AddClassHandler<ToolBar>((x, e) => { if (x.ViewModel is not null) x.ViewModel.TabsVisible = e.GetNewValue<bool>(); });

            // Global UI -> Core Triangulation: GroupSpacing now writes directly to the global manager root!
            GroupSpacingProperty.Changed.AddClassHandler<ToolBar, double>((x, e) => { FlexLayoutManager.Instance.GroupSpacing = e.NewValue.Value; });
            ActiveThemeNameProperty.Changed.AddClassHandler<ToolBar, string>((x, e) => { FlexLayoutManager.Instance.ActiveThemeName = e.NewValue.Value; });

            ToolBarIdProperty.Changed.AddClassHandler<ToolBar>((x, e) => x.OnToolBarIdChanged(e.GetNewValue<string?>()));
        }

        public FlexToolBarViewModel? ViewModel { get; private set; }

        public ToolBar()
        {
            SetValue(TabsProperty, new ObservableCollection<Tab>());
            ResetLayoutCommand = new MiniRelayCommand(() => FlexLayoutManager.DeleteLayout());

            if (Tabs != null) Tabs.CollectionChanged += OnTabsCollectionChanged;

            InitializeStaticSaveBridge();

            FlexLayoutManager.LayoutResetRequested += ResetToDefaultLayout;
        }

        public ICommand ResetLayoutCommand { get; }

        // Synchronous Context Assembly and Core Data Mapping Engine
        private void OnToolBarIdChanged(string? newId)
        {
            if (string.IsNullOrWhiteSpace(newId)) return;

            // Strict Memory Hygiene: Instantly detach old reactive hooks to completely eliminate memory leaks
            if (ViewModel != null) ViewModel.PropertyChanged -= OnCoreModelPropertyChanged;
            FlexLayoutManager.Instance.PropertyChanged -= OnGlobalManagerPropertyChanged;

            // Core Factory Gateway: Extracts or safely constructs the active view model tracking configuration updates
            var targetModel = FlexLayoutManager.GetToolBar(newId);
            if (targetModel == null) return;

            // Cold Start Interception: Fresh runtime model absorbs unique inline declarative XAML default specifications
            if (targetModel.IsNew)
            {
                targetModel.IsSingleExpandGroup = this.IsSingleExpandGroup;
                targetModel.TabStripVisible = this.TabStripVisible;
                targetModel.TabsVisible = this.TabsVisible;
            }

            ViewModel = targetModel;

            // Synchronous Theme Pre-Loading: Ensures physical template assets parse completely BEFORE layout properties overlap
            ApplyThemeDirect(FlexLayoutManager.Instance.ActiveThemeName);
            SetCurrentValue(ActiveThemeNameProperty, FlexLayoutManager.Instance.ActiveThemeName);

            // Hot Start Reconstruction: Overrides underlying low-priority style metrics using high-priority values loaded from persistent JSON records
            if (!ViewModel.IsNew)
            {
                SetCurrentValue(GroupSpacingProperty, FlexLayoutManager.Instance.GroupSpacing);
                SetCurrentValue(IsSingleExpandGroupProperty, ViewModel.IsSingleExpandGroup);
                SetCurrentValue(TabStripVisibleProperty, ViewModel.TabStripVisible);
                SetCurrentValue(TabsVisibleProperty, ViewModel.TabsVisible);
            }

            // Arm Parallel Communication Channels: Activate live reactive loops now that initialization sequence is entirely complete
            ViewModel.PropertyChanged += OnCoreModelPropertyChanged;
            FlexLayoutManager.Instance.PropertyChanged += OnGlobalManagerPropertyChanged;
        }

        // Core -> UI Direction: Translates internal core state alterations to the layout engine without destroying third-party developer bindings
        private void OnCoreModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (ViewModel == null) return;

            switch (args.PropertyName)
            {
                case nameof(ViewModel.IsSingleExpandGroup):
                    SetCurrentValue(IsSingleExpandGroupProperty, ViewModel.IsSingleExpandGroup);
                    break;
                case nameof(ViewModel.TabStripVisible):
                    SetCurrentValue(TabStripVisibleProperty, ViewModel.TabStripVisible);
                    break;
                case nameof(ViewModel.TabsVisible):
                    SetCurrentValue(TabsVisibleProperty, ViewModel.TabsVisible);
                    break;
            }
        }

        // Global Manager -> UI Direction: Safely relays global configuration metrics down into individual instance frames
        private void OnGlobalManagerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(FlexLayoutManager.Instance.ActiveThemeName))
            {
                ApplyThemeDirect(FlexLayoutManager.Instance.ActiveThemeName);
                SetCurrentValue(ActiveThemeNameProperty, FlexLayoutManager.Instance.ActiveThemeName);
            }
            else if (args.PropertyName == nameof(FlexLayoutManager.Instance.GroupSpacing))
            {
                SetCurrentValue(GroupSpacingProperty, FlexLayoutManager.Instance.GroupSpacing);
            }
        }

        private static void InitializeStaticSaveBridge()
        {
            if (_isSaveBridgeSubscribed) return;
            _isSaveBridgeSubscribed = true;

            _globalSaveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };

            _globalSaveTimer.Tick += (s, e) =>
            {
                _globalSaveTimer.Stop();
                FlexLayoutManager.SaveLayout();
            };

            FlexLayoutManager.Instance.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(FlexLayoutManager.Instance.IsEdited))
                {
                    if (FlexLayoutManager.Instance.IsEdited)
                    {
                        _globalSaveTimer.Stop();
                        _globalSaveTimer.Start();
                    }
                }
            };
        }

        private void SetTheme(string selectedTheme)
        {
            if (ToolBarThemeManager.TryGetThemeUri(selectedTheme, out var targetUri) && targetUri != null)
            {
                try
                {
                    var styleInclude = new global::Avalonia.Markup.Xaml.Styling.StyleInclude(targetUri) { Source = targetUri };
                    this.Styles.Add(styleInclude);
                }
                catch (Exception) { }
            }
        }

        private void ApplyThemeDirect(string selectedTheme)
        {
            if (string.IsNullOrEmpty(selectedTheme) || selectedTheme == _currentlyLoadedThemeName && Styles.Count > 0) return;

            _currentlyLoadedThemeName = selectedTheme;
            this.Styles.Clear();

            SetTheme("Default");
            if (selectedTheme == "Default") return;

            SetTheme(selectedTheme);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _tabStrip = e.NameScope.Find<TabStrip>("PART_TabSelectionStrip");
            if (_tabStrip != null)
            {
                _tabStrip.SelectionChanged += (s, args) =>
                {
                    if (RestoreSelectedTab && _tabStrip.SelectedItem is Tab activeUiTab && ViewModel != null)
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

            if (!string.IsNullOrWhiteSpace(ToolBarId))
            {
                _parentWindow = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();
                if (_parentWindow != null) _parentWindow.Closing += OnParentWindowClosing;
            }
        }

        protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (ViewModel == null) return;

            foreach (var uiTab in Tabs)
            {
                if (uiTab.Items == null) continue;

                foreach (var item in uiTab.Items)
                {
                    if (item is FlexGroup uiGroup)
                    {
                        var groupModel = ViewModel.GetGroup(uiGroup.GroupId);
                        if (groupModel == null) continue;
                        groupModel.TabId = Tab.GetTabId(uiTab);

                        uiGroup.BindToCoreModel(groupModel);
                    }
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (ViewModel != null) ViewModel.PropertyChanged -= OnCoreModelPropertyChanged;
            FlexLayoutManager.Instance.PropertyChanged -= OnGlobalManagerPropertyChanged;
            FlexLayoutManager.LayoutResetRequested -= ResetToDefaultLayout;

            if (_parentWindow != null) _parentWindow.Closing -= OnParentWindowClosing;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnParentWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            FlexLayoutManager.SaveLayout();
        }

        public void ResetToDefaultLayout()
        {
            this.ClearValue(GroupSpacingProperty);
            this.ClearValue(IsSingleExpandGroupProperty);
            this.ClearValue(ActiveThemeNameProperty);
            this.ClearValue(TabStripVisibleProperty);
            this.ClearValue(TabsVisibleProperty);

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
