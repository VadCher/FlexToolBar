using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FlexToolBar.Core;

/// <summary>
/// Represents the state of a single toolbar group.
/// </summary>
public class FlexGroupState
{
    public string GroupId { get; set; } = string.Empty;
    public bool IsExpanded { get; set; }
    public bool IsPinned { get; set; }
}

/// <summary>
/// Represents the overall layout state of the toolbar.
/// </summary>
public class FlexToolbarState
{
    public string SelectedTabId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether single expansion mode is enabled.
    /// </summary>
    public bool IsSingleExpandMode { get; set; }
    
    public List<FlexGroupState> Groups { get; set; } = new();
}

/// <summary>
/// Manages saving and loading of the toolbar layout state.
/// </summary>
public class FlexLayoutManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string SaveLayout(IFlexToolBarViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var state = new FlexToolbarState
        {
            IsSingleExpandMode = viewModel.IsSingleExpandGroup,
            SelectedTabId = viewModel.SelectedTabId
        };

        foreach (var tab in viewModel.Tabs)
        {
            foreach (var group in tab.Groups)
            {
                if (!string.IsNullOrEmpty(group.GroupId))
                {
                    state.Groups.Add(new FlexGroupState
                    {
                        GroupId = group.GroupId,
                        IsExpanded = group.IsExpanded,
                        IsPinned = group.IsPinned
                    });
                }
            }
        }

        return JsonSerializer.Serialize(state, SerializerOptions);
    }

    public void LoadLayout(IFlexToolBarViewModel viewModel, string json)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (string.IsNullOrWhiteSpace(json)) return;

        FlexToolbarState? state;
        try
        {
            state = JsonSerializer.Deserialize<FlexToolbarState>(json, SerializerOptions);
        }
        catch (JsonException) { return; }

        if (state == null) return;
        
        // RESTORE: Synchronize the selected tab identifier back to core view model
        viewModel.SelectedTabId = state.SelectedTabId;
        viewModel.IsSingleExpandGroup = state.IsSingleExpandMode;

        if (state.Groups == null) return;

        var groupStateMap = state.Groups
            .Where(g => !string.IsNullOrEmpty(g.GroupId))
            .ToDictionary(g => g.GroupId, g => g);

        foreach (var tab in viewModel.Tabs)
        {
            foreach (var group in tab.Groups)
            {
                if (!string.IsNullOrEmpty(group.GroupId) && groupStateMap.TryGetValue(group.GroupId, out var groupState))
                {
                    group.IsExpanded = groupState.IsExpanded;
                    group.IsPinned = groupState.IsPinned;
                }
            }
        }
    }
}
