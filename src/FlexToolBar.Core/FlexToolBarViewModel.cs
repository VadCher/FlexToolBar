using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlexToolBar.Core
{
    public class FlexToolBarViewModel : ViewModelBase
    {
        public Dictionary<string, FlexGroupViewModel> Groups { get; set; } = new();

        public FlexGroupViewModel GetGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return new FlexGroupViewModel();
            FlexGroupViewModel? groupModel = default;
            if (!Groups.TryGetValue(groupId, out groupModel))
            {
                groupModel = new FlexGroupViewModel() { IsNew = true };
                Groups[groupId] = groupModel;
            }

            groupModel.SetParent(this);

            return groupModel;
        }

        public double GroupSpacing
        {
            get => field;
            set => RaiseAndSetIfChanged(ref field, value);
        } = 6.0;

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
