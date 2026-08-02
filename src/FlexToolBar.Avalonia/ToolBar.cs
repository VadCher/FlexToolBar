using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace FlexToolBar.Avalonia;

/// <summary>
/// Represents the root toolbar control hosting multiple tabs and managing group expansion behavior.
/// </summary>
public class ToolBar : TemplatedControl
{
    /// <summary>
    /// Defines the <see cref="IsSingleExpandGroup"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSingleExpandGroupProperty =
        AvaloniaProperty.Register<ToolBar, bool>(
            nameof(IsSingleExpandGroup),
            defaultValue: false);

    /// <summary>
    /// Defines the <see cref="Tabs"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Tab>> TabsProperty =
        AvaloniaProperty.Register<ToolBar, ObservableCollection<Tab>>(
            nameof(Tabs),
            defaultValue: new ObservableCollection<Tab>());

    private static readonly DirectProperty<ToolBar, bool> IsTabHeaderVisibleProperty =
        AvaloniaProperty.RegisterDirect<ToolBar, bool>(
            nameof(IsTabHeaderVisible),
            o => o.IsTabHeaderVisible);

    private bool _isTabHeaderVisible;

    static ToolBar()
    {
        TabsProperty.Changed.AddClassHandler<ToolBar>((x, e) => x.OnTabsChanged(e));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolBar"/> class.
    /// </summary>
    public ToolBar()
    {
        UpdateTabHeaderVisibility();
    }

    /// <summary>
    /// Gets or sets a value indicating whether only one unpinned group can be expanded at a time.
    /// </summary>
    public bool IsSingleExpandGroup
    {
        get => GetValue(IsSingleExpandGroupProperty);
        set => SetValue(IsSingleExpandGroupProperty, value);
    }

    /// <summary>
    /// Gets or sets the collection of tabs hosted within the toolbar.
    /// </summary>
    public ObservableCollection<Tab> Tabs
    {
        get => GetValue(TabsProperty);
        set => SetValue(TabsProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the tab header selection strip is visible.
    /// </summary>
    public bool IsTabHeaderVisible
    {
        get => _isTabHeaderVisible;
        private set => SetAndRaise(IsTabHeaderVisibleProperty, ref _isTabHeaderVisible, value);
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
        UpdateTabHeaderVisibility();
    }

    private void UpdateTabHeaderVisibility()
    {
        IsTabHeaderVisible = Tabs != null && Tabs.Count > 1;
    }
}
