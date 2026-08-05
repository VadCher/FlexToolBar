using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents a tab container control holding a collection of FlexGroup items.
    /// Supports automated snap-to-group touch friendly side buttons scrolling.
    /// </summary>
    public class Tab : HeaderedItemsControl
    {
        private ScrollViewer? _scrollViewer;

        public static readonly AttachedProperty<string> TabIdProperty =
            AvaloniaProperty.RegisterAttached<Tab, AvaloniaObject, string>("TabId", string.Empty);

        public static string GetTabId(AvaloniaObject element) => element.GetValue(TabIdProperty);
        public static void SetTabId(AvaloniaObject element, string value) => element.SetValue(TabIdProperty, value);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_TabScrollViewer");

            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            }
        }

        private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null) return;

            double currentX = _scrollViewer.Offset.X;
            double maxScrollableX = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;

        }
    }
}
