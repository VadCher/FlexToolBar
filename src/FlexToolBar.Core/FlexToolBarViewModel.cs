using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlexToolBar.Core
{
    public class FlexToolBarViewModel : ViewModelBase
    {
        public Dictionary<string, FlexGroupViewModel> Groups { get; set; } = new();

        public FlexGroupViewModel? GetGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;

            if (!Groups.TryGetValue(groupId, out var groupModel))
            {
                groupModel = new FlexGroupViewModel { IsNew = true };
                Groups[groupId] = groupModel;
            }

            groupModel.SetParent(this);

            return groupModel;
        }

        public bool TabStripVisible
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        } = true;

        public bool TabsVisible
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        } = true;

        public string SelectedTabId
        {
            get;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                RaiseAndSetIfChanged(ref field, value);
            }
        } = string.Empty;

        public bool IsSingleExpandGroup
        {
            get;
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
