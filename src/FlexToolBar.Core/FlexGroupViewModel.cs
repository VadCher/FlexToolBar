using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FlexToolBar.Core;

/// <summary>
/// Represents the implementation of the IFlexGroupViewModel interface.
/// </summary>
public class FlexGroupViewModel : ViewModelBase, IFlexGroupViewModel
{
    private string _groupId;
    private string _header;
    private string? _expandedHeader;
    private object? _icon;
    private bool _isExpanded = true;
    private bool _isPinned = false;
    private bool _pinVisible = true;

    /// <summary>
    /// Initializes a new instance of the FlexGroupViewModel class.
    /// </summary>
    /// <param name="groupId">The unique identifier for the group.</param>
    /// <param name="header">The display header for the group.</param>
    public FlexGroupViewModel(string groupId, string header)
    {
        _groupId = groupId;
        _header = header;
    }

    /// <inheritdoc />
    public string GroupId
    {
        get => _groupId;
        init => SetProperty(ref _groupId, value);
    }

    /// <inheritdoc />
    public string Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }

    /// <inheritdoc />
    public string? ExpandedHeader
    {
        get => _expandedHeader;
        set => SetProperty(ref _expandedHeader, value);
    }

    /// <inheritdoc />
    public object? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    /// <inheritdoc />
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <inheritdoc />
    public bool IsPinned
    {
        get => _isPinned;
        set => SetProperty(ref _isPinned, value);
    }

    /// <inheritdoc />
    public bool PinVisible
    {
        get => _pinVisible;
        set => SetProperty(ref _pinVisible, value);
    }

    /// <inheritdoc />
    public ObservableCollection<object> Items { get; } = new();
}
