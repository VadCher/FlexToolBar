using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FlexToolBar.Core;

/// <summary>
/// Represents the root view model interface for the FlexToolBar component.
/// </summary>
public interface IFlexToolBarViewModel
{
    /// <summary>
    /// Gets or sets a value indicating whether only one group can be expanded at a time.
    /// </summary>
    bool IsSingleExpandGroup { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the currently selected tab.
    /// </summary>
    public string SelectedTabId { get; set; }

    /// <summary>
    /// Gets the collection of available toolbar tabs.
    /// </summary>
    ObservableCollection<IFlexTabViewModel> Tabs { get; }

    /// <summary>
    /// Gets the command to reset all groups to their default states.
    /// </summary>
    ICommand ResetLayoutCommand { get; }
}
