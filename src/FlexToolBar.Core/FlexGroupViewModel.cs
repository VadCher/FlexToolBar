using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace FlexToolBar.Core
{
    /// <summary>
    /// Represents the cross-platform technical state tracking view model for a single functional group container.
    /// </summary>
    public class FlexGroupViewModel : ViewModelBase
    {
        private FlexToolBarViewModel? _parent;
        public FlexGroupViewModel() { }
        /// <summary>
        /// Gets or sets the unique string identifier of the tab hosting this group.
        /// Excluded from JSON to maintain a lean schema.
        /// </summary>
        [JsonIgnore]
        public string TabId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display header for the group.
        /// </summary>
        [JsonIgnore]
        public string Header
        {
            get => field;
            set => RaiseAndSetIfChanged(ref field, value);
        } = string.Empty;

        /// <summary>
        /// Gets or sets the secondary header displayed when the group is expanded.
        /// </summary>
        [JsonIgnore]
        public string? ExpandedHeader
        {
            get => field;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets or sets the visual icon asset representing the group in its collapsed state.
        /// </summary>
        [JsonIgnore]
        public object? Icon
        {
            get => field;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the pinning toggle button is visible.
        /// </summary>
        [JsonIgnore]
        public bool PinVisible
        {
            get => field;
            set => RaiseAndSetIfChanged(ref field, value);
        } = true;

        /// <summary>
        /// Gets the dynamic collection of child interactive elements hosted inside this group workspace.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<object> Items { get; } = new();

        /// <summary>
        /// Gets or sets whether this group panel layout container is currently expanded on screen.
        /// </summary>
        public bool IsExpanded
        {
            get => field;
            set
            {
                // VADIM'S SPECIFICATION: Self-contained tab-boundary single expand execution engine
                if (value && !field && _parent != null && _parent.IsSingleExpandGroup)
                {
                    foreach (var kp in _parent.Groups)
                    {
                        var target = kp.Value;
                        if (target != this && target.TabId == this.TabId && !target.IsPinned && target.IsExpanded)
                        {
                            target.IsExpanded = false;
                        }
                    }
                }
                RaiseAndSetIfChanged(ref field, value);
            }
        } = true;

        /// <summary>
        /// Gets or sets whether this group is pinned, preventing automatic or manual collapsing actions.
        /// </summary>
        public bool IsPinned
        {
            get => field;
            set => RaiseAndSetIfChanged(ref field, value);
        } = false;

        /// <summary>
        /// Internally binds the group instance back to its root execution toolbar controller context.
        /// </summary>
        public void SetParent(FlexToolBarViewModel parentViewModel)
        {
            _parent = parentViewModel;
        }
    }
}
