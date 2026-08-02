using System.Collections.ObjectModel;

namespace FlexToolBar.Core;

/// <summary>
/// Represents a toolbar group view model containing inner toolbar elements.
/// </summary>
public interface IFlexGroupViewModel
{
    /// <summary>
    /// Gets the unique identifier used for JSON layout serialization.
    /// </summary>
    string GroupId { get; }

    /// <summary>
    /// Gets or sets the display name for the collapsed button state.
    /// </summary>
    string Header { get; set; }

    /// <summary>
    /// Gets or sets the display name for the expanded state, or null for fallback.
    /// </summary>
    string? ExpandedHeader { get; set; }

    /// <summary>
    /// Gets or sets the abstract icon representation for the group.
    /// </summary>
    object? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the group is currently expanded.
    /// </summary>
    bool IsExpanded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the group is currently pinned.
    /// </summary>
    bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pin button is visible.
    /// </summary>
    bool PinVisible { get; set; }

    /// <summary>
    /// Gets the collection of custom inner UI elements.
    /// </summary>
    ObservableCollection<object> Items { get; }
}
