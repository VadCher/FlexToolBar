using System.Collections.ObjectModel;

namespace FlexToolBar.Core;

/// <summary>
/// Represents a single tab view model containing a collection of toolbar groups.
/// </summary>
public interface IFlexTabViewModel
{
    /// <summary>
    /// Gets the unique identifier used for JSON serialization.
    /// </summary>
    string TabId { get; }

    /// <summary>
    /// Gets or sets the tab header display text.
    /// </summary>
    string Header { get; set; }

    /// <summary>
    /// Gets the collection of child groups inside this tab.
    /// </summary>
    ObservableCollection<IFlexGroupViewModel> Groups { get; }
}
