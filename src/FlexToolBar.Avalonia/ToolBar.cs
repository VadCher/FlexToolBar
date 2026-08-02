using System.Collections.ObjectModel;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolBar"/> class.
    /// </summary>
    public ToolBar()
    {
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
}
