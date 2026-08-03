using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents a smart responsive container control switching between collapsed and expanded states.
    /// Supports caching of compile-time XAML defaults for resilient factory resets.
    /// </summary>
    public class FlexGroup : ContentControl
    {
        private bool _xamlDefaultIsExpanded = true;
        private bool _xamlDefaultIsPinned = false;

        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<FlexGroup, string>(nameof(Header), string.Empty);

        public static readonly StyledProperty<string?> ExpandedHeaderProperty =
            AvaloniaProperty.Register<FlexGroup, string?>(nameof(ExpandedHeader), null);

        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<FlexGroup, object?>(nameof(Icon), null);

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(
                nameof(IsExpanded), 
                true, 
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsPinnedProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(IsPinned), false);

        public static readonly StyledProperty<bool> PinVisibleProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(PinVisible), true);

        public static readonly AttachedProperty<string> GroupIdProperty =
            AvaloniaProperty.RegisterAttached<FlexGroup, AvaloniaObject, string>("GroupId", string.Empty);

        static FlexGroup()
        {
            IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.OnIsExpandedChanged(e));
            IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.OnIsPinnedChanged(e));
        }

        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public string? ExpandedHeader
        {
            get => GetValue(ExpandedHeaderProperty);
            set => SetValue(ExpandedHeaderProperty, value);
        }

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public bool IsPinned
        {
            get => GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        public bool PinVisible
        {
            get => GetValue(PinVisibleProperty);
            set => SetValue(PinVisibleProperty, value);
        }

        /// <summary>
        /// Gets the cached original compile-time XAML expansion default state.
        /// </summary>
        public bool XamlDefaultIsExpanded => _xamlDefaultIsExpanded;

        /// <summary>
        /// Gets the cached original compile-time XAML pinning default state.
        /// </summary>
        public bool XamlDefaultIsPinned => _xamlDefaultIsPinned;

        public static string GetGroupId(AvaloniaObject element) => element.GetValue(GroupIdProperty);

        public static void SetGroupId(AvaloniaObject element, string value) => element.SetValue(GroupIdProperty, value);

        /// <inheritdoc />
        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // CRITICAL: Cache the pure XAML layout configurations assigned by the app developer 
            // before any runtime user mutations or serialization files override them
            _xamlDefaultIsExpanded = IsExpanded;
            _xamlDefaultIsPinned = IsPinned;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var closeButton = e.NameScope.Find<Button>("PART_CloseButton");
            if (closeButton != null)
            {
                closeButton.Click += (s, args) =>
                {
                    if (!IsPinned) IsExpanded = false;
                };
            }

            var collapsedButton = e.NameScope.Find<Button>("PART_CollapsedButton");
            if (collapsedButton != null)
            {
                collapsedButton.Click += (s, args) => IsExpanded = true;
            }

            UpdatePseudoClasses();
        }

        private void OnIsExpandedChanged(AvaloniaPropertyChangedEventArgs e) => UpdatePseudoClasses();
        private void OnIsPinnedChanged(AvaloniaPropertyChangedEventArgs e) => UpdatePseudoClasses();

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":expanded", IsExpanded);
            PseudoClasses.Set(":collapsed", !IsExpanded);
            PseudoClasses.Set(":pinned", IsPinned);
        }
    }
}
