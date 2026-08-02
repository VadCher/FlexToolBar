using System.Collections.ObjectModel;

namespace FlexToolBar.Core;

/// <summary>
/// Represents the implementation of the IFlexTabViewModel interface.
/// </summary>
public class FlexTabViewModel : ViewModelBase, IFlexTabViewModel
{
    private string _tabId;
    private string _header;

    /// <summary>
    /// Initializes a new instance of the FlexTabViewModel class.
    /// </summary>
    /// <param name="tabId">The unique identifier for the tab.</param>
    /// <param name="header">The display header for the tab.</param>
    public FlexTabViewModel(string tabId, string header)
    {
        _tabId = tabId;
        _header = header;
    }

    /// <inheritdoc />
    public string TabId
    {
        get => _tabId;
        init => SetProperty(ref _tabId, value);
    }

    /// <inheritdoc />
    public string Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }

    /// <inheritdoc />
    public ObservableCollection<IFlexGroupViewModel> Groups { get; } = new();
}
