using System;
using System.Collections.Generic;

namespace FlexToolBar.Core
{
    public class FlexToolBarViewModel : ViewModelBase
    {
        public Dictionary<string, FlexGroupViewModel> Groups { get; set; } = new();

        public void RegisterGroup(string groupId, FlexGroupViewModel groupViewModel)
        {
            if (string.IsNullOrEmpty(groupId) || groupViewModel == null) return;
            
            groupViewModel.SetParent(this);
            Groups[groupId] = groupViewModel;
        }

        public double GroupSpacing
        {
            get => field;
            set => RaiseAndSetIfChanged(ref field, value);
        } = 6.0;

        public string ActiveThemeName
        {
            get => field;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                RaiseAndSetIfChanged(ref field, value);
            }
        } = "Default";

        public string SelectedTabId
        {
            get => field;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                RaiseAndSetIfChanged(ref field, value);
            }
        } = string.Empty;

        public string PanelEdge
        {
            get => field;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                RaiseAndSetIfChanged(ref field, value);
            }
        } = "Top";

        public bool IsSingleExpandGroup
        {
            get => field;
            set
            {
                if (RaiseAndSetIfChanged(ref field, value) && value)
                {
                    CollapseGroupsToSingleMode();
                }
            }
        } = false;

        private void CollapseGroupsToSingleMode()
        {
            var activeTabsTracker = new HashSet<string>();

            foreach (var kp in Groups)
            {
                var group = kp.Value;
                if (string.IsNullOrEmpty(group.TabId)) continue;

                if (group.IsExpanded && !group.IsPinned)
                {
                    if (activeTabsTracker.Contains(group.TabId))
                    {
                        group.IsExpanded = false;
                    }
                    else
                    {
                        activeTabsTracker.Add(group.TabId);
                    }
                }
            }
        }
    }
}
