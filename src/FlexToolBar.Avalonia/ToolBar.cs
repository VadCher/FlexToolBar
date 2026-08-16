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
        private bool _xamlDefaultIsSingleExpand = false;
        private string _currentlyLoadedThemeName = "Default";
        private bool _isTabHeaderVisible;
        private readonly Dictionary<string, object> _themeRegistry = new();

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

        public static readonly StyledProperty<ObservableCollection<Tab>?> TabsProperty =
            AvaloniaProperty.Register<ToolBar, ObservableCollection<Tab>?>(nameof(Tabs), defaultValue: null);

        public static readonly StyledProperty<string?> ToolBarIdProperty =
            AvaloniaProperty.Register<ToolBar, string?>(nameof(ToolBarId), defaultValue: null);

        public string? ToolBarId
        {
            get => GetValue(ToolBarIdProperty);
            set => SetValue(ToolBarIdProperty, value);
        }

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

            // Automated context hydration pipeline bound natively to runtime XML parsing stages
            ToolBarIdProperty.Changed.AddClassHandler<ToolBar>((x, e) => x.OnToolBarIdChanged(e.GetNewValue<string?>()));
        }
        /// <summary>
        /// Accessor token exposing the active core view model resolved from the global store registry.
        /// </summary>
        public FlexToolBarViewModel? ViewModel { get; private set; }

        public ToolBar()
        {
            SetValue(TabsProperty, new ObservableCollection<Tab>());
            ResetLayoutCommand = new MiniRelayCommand(() => FlexLayoutManager.DeleteLayout());

            if (Tabs != null) Tabs.CollectionChanged += OnTabsCollectionChanged;
            UpdateTabHeaderVisibility();

            AvailableThemes = ToolBarThemeManager.AvailableThemes;

            InitializeStaticSaveBridge();

            FlexLayoutManager.LayoutResetRequested += ResetToDefaultLayout;
        }

        private void OnLayoutResetRequested()
        {
            Dispatcher.UIThread.Post(ResetToDefaultLayout, DispatcherPriority.Background);
        }

        // The Smart Declarative Context Hydration Pipeline
        private void OnToolBarIdChanged(string? newId)
        {
            if (string.IsNullOrWhiteSpace(newId)) return;

            // Extract or build the model directly inside our centralized memory store container
            ViewModel = FlexLayoutManager.GetToolBar(newId);

            // Setup direct TwoWay reactive sync loops straight into the live model fields
            this.Bind(IsSingleExpandGroupProperty, new global::Avalonia.Data.Binding(nameof(ViewModel.IsSingleExpandGroup)) { Source = ViewModel, Mode = global::Avalonia.Data.BindingMode.TwoWay });
            this.Bind(GroupSpacingProperty, new global::Avalonia.Data.Binding(nameof(ViewModel.GroupSpacing)) { Source = ViewModel, Mode = global::Avalonia.Data.BindingMode.TwoWay });
            this.Bind(ActiveThemeNameProperty, new global::Avalonia.Data.Binding(nameof(FlexLayoutManager.ActiveThemeName)) { Source = FlexLayoutManager.Instance, Mode = global::Avalonia.Data.BindingMode.TwoWay });

            // Ensure the active rendering engine immediately paints the cached workspace styles
            ApplyThemeDirect(FlexLayoutManager.Instance.ActiveThemeName);
        }

        // Autonomous UI-Thread Debounced Save Bridge (Protects core models from thread race conditions)
        private static void InitializeStaticSaveBridge()
        {
            if (_isSaveBridgeSubscribed) return;
            _isSaveBridgeSubscribed = true;

            _globalSaveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(2.5) // Safe cooldown window before serialization
            };

            _globalSaveTimer.Tick += (s, e) =>
            {
                _globalSaveTimer.Stop();
                FlexLayoutManager.SaveLayout(); // Commits the entire dictionary data block sequentially
            };

            FlexLayoutManager.Instance.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(FlexLayoutManager.Instance.IsEdited))
                {
                    if (FlexLayoutManager.Instance.IsEdited)
                    {
                        // Reset the debounce window clocks natively upon user interface changes
                        _globalSaveTimer.Stop();
                        _globalSaveTimer.Start();
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
            if (string.IsNullOrEmpty(selectedTheme) || selectedTheme == _currentlyLoadedThemeName && Styles.Count>0) return;

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
            _xamlDefaultIsSingleExpand = IsSingleExpandGroup;

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
                        groupModel.TabId = Tab.GetTabId(uiTab);

                        uiGroup.BindToCoreModel(groupModel);
                    }
                }
            }
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            FlexLayoutManager.LayoutResetRequested -= ResetToDefaultLayout;
            if (_parentWindow != null) _parentWindow.Closing -= OnParentWindowClosing;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnParentWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // HARDWARE SHIELD: Forces an instantaneous atomized save transaction when user closes the app
            FlexLayoutManager.SaveLayout();
        }

        public void ResetToDefaultLayout()
        {
            this.ClearValue(GroupSpacingProperty);
            this.ClearValue(IsSingleExpandGroupProperty);
            this.ClearValue(ActiveThemeNameProperty);


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
