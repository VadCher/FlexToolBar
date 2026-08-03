using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

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
        AvaloniaProperty.Register<ToolBar, bool>(nameof(IsSingleExpandGroup), defaultValue: false);

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
        
        // Trigger 1: Force instant collapse of sibling groups when the single expand mode is turned on
        IsSingleExpandGroupProperty.Changed.AddClassHandler<ToolBar>((toolBar, e) =>
        {
            if (e.NewValue is true)
            {
                toolBar.EnforceSingleExpandLayout();
            }
        });

        // Trigger 2: Listen to IsExpanded changes on any FlexGroup within our UI hierarchy
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolBar"/> class.
    /// </summary>
    public ToolBar()
    {
        // CRITICAL: Immediately subscribe to the default collection items changes 
        // to detect tabs added via declarative XAML markup at startup
        if (Tabs != null)
        {
            Tabs.CollectionChanged += OnTabsCollectionChanged;
        }
        
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

    private void EnforceSingleExpandLayout()
    {
        if (Tabs == null) return;

        foreach (var tab in Tabs)
        {
            bool foundFirstDynamic = false;
            foreach (var item in tab.Items)
            {
                if (item is FlexGroup group && !group.IsPinned)
                {
                    if (!foundFirstDynamic && group.IsExpanded)
                    {
                        foundFirstDynamic = true;
                    }
                    else
                    {
                        group.IsExpanded = false;
                    }
                }
            }
        }
    }

    private void CollapseSiblingGroups(FlexGroup activeGroup)
    {
        var targetTab = activeGroup.GetLogicalAncestors().OfType<Tab>().FirstOrDefault();
        if (targetTab == null) return;

        foreach (var item in targetTab.Items)
        {
            if (item is FlexGroup sibling && sibling != activeGroup && !sibling.IsPinned)
            {
                sibling.IsExpanded = false;
            }
        }
    }
}
