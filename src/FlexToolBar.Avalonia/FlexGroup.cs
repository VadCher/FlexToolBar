using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FlexToolBar.Avalonia;

/// <summary>
/// Represents a smart container group control that can be expanded, collapsed, and pinned.
/// </summary>
public class FlexGroup : ContentControl
{
    /// <summary>
    /// Defines the <see cref="Header"/> styled property.
    /// </summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<FlexGroup, string?>(nameof(Header));

    /// <summary>
    /// Defines the <see cref="ExpandedHeader"/> styled property.
    /// </summary>
    public static readonly StyledProperty<string?> ExpandedHeaderProperty =
        AvaloniaProperty.Register<FlexGroup, string?>(nameof(ExpandedHeader));

    /// <summary>
    /// Defines the <see cref="Icon"/> styled property.
    /// </summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<FlexGroup, object?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="IsExpanded"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<FlexGroup, bool>(
            nameof(IsExpanded),
            defaultValue: true,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="IsPinned"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> IsPinnedProperty =
        AvaloniaProperty.Register<FlexGroup, bool>(
            nameof(IsPinned),
            defaultValue: false,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="PinVisible"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> PinVisibleProperty =
        AvaloniaProperty.Register<FlexGroup, bool>(
            nameof(PinVisible),
            defaultValue: true);

    static FlexGroup()
    {
        IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.OnIsExpandedChanged(e));
        IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.OnIsPinnedChanged(e));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlexGroup"/> class.
    /// </summary>
    public FlexGroup()
    {
        UpdatePseudoClasses(IsExpanded, IsPinned);
    }

    /// <summary>
    /// Gets or sets the header text for the collapsed state button.
    /// </summary>
    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the expanded header text for the bottom of the expanded panel.
    /// </summary>
    public string? ExpandedHeader
    {
        get => GetValue(ExpandedHeaderProperty);
        set => SetValue(ExpandedHeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon displayed in the collapsed state.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the group is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the group is pinned.
    /// </summary>
    public bool IsPinned
    {
        get => GetValue(IsPinnedProperty);
        set => SetValue(IsPinnedProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pin toggle button is visible.
    /// </summary>
    public bool PinVisible
    {
        get => GetValue(PinVisibleProperty);
        set => SetValue(PinVisibleProperty, value);
    }

    private void OnIsExpandedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isExpanded)
        {
            UpdatePseudoClasses(isExpanded, IsPinned);
        }
    }

    private void OnIsPinnedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isPinned)
        {
            UpdatePseudoClasses(IsExpanded, isPinned);
        }
    }

    private void UpdatePseudoClasses(bool isExpanded, bool isPinned)
    {
        PseudoClasses.Set(":expanded", isExpanded);
        PseudoClasses.Set(":collapsed", !isExpanded);
        PseudoClasses.Set(":pinned", isPinned);
    }
}
