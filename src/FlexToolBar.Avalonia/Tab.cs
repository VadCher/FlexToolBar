using Avalonia;
using Avalonia.Controls.Primitives;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents a tab container control holding a collection of FlexGroup items.
    /// </summary>
    public class Tab : HeaderedItemsControl
    {
        /// <summary>
        /// Defines the TabId attached property.
        /// </summary>
        public static readonly AttachedProperty<string> TabIdProperty =
            AvaloniaProperty.RegisterAttached<Tab, AvaloniaObject, string>("TabId", string.Empty);

        /// <summary>
        /// Accessor for Attached Property TabId.
        /// </summary>
        public static string GetTabId(AvaloniaObject element) => element.GetValue(TabIdProperty);

        /// <summary>
        /// Accessor for Attached Property TabId.
        /// </summary>
        public static void SetTabId(AvaloniaObject element, string value) => element.SetValue(TabIdProperty, value);
    }
}
