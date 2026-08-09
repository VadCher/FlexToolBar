using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace FlexToolBar.Avalonia
{
    public class ToolBarSettingsFlexGroup : FlexGroup
    {
        private ToolBar? _parentToolBar;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _parentToolBar = this.FindLogicalAncestorOfType<ToolBar>();
            
            var resetBtn = e.NameScope.Find<Button>("PART_ResetStyleButton");
            if (resetBtn != null)
            {
                resetBtn.Click += OnResetStyleClick;
            }
        }

        private void OnResetStyleClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            _parentToolBar?.ResetToDefaultLayout();
        }
    }
}
