using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents a smart responsive container control switching between collapsed and expanded states.
    /// </summary>
    public class FlexGroup : ContentControl
    {
        /// <summary>
        /// Defines the Header styled property.
        /// </summary>
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<FlexGroup, string>(nameof(Header), string.Empty);

        /// <summary>
        /// Defines the ExpandedHeader styled property.
        /// </summary>
        public static readonly StyledProperty<string?> ExpandedHeaderProperty =
            AvaloniaProperty.Register<FlexGroup, string?>(nameof(ExpandedHeader), null);

        /// <summary>
        /// Defines the Icon styled property.
        /// </summary>
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<FlexGroup, object?>(nameof(Icon), null);

        /// <summary>
        /// Defines the IsExpanded styled property with enabled TwoWay binding by default.
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(IsExpanded), true, defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the IsPinned styled property.
        /// </summary>
        public static readonly StyledProperty<bool> IsPinnedProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(IsPinned), false);

        /// <summary>
        /// Defines the PinVisible styled property.
        /// </summary>
        public static readonly StyledProperty<bool> PinVisibleProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(PinVisible), true);

        /// <summary>
        /// Defines the GroupId attached property.
        /// </summary>
        public static readonly AttachedProperty<string> GroupIdProperty =
            AvaloniaProperty.RegisterAttached<FlexGroup, AvaloniaObject, string>("GroupId", string.Empty);

        static FlexGroup()
        {
            // Register pseudo-classes triggers based on property changes
            IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.OnIsExpandedChanged(e));
            IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.OnIsPinnedChanged(e));
        }

        /// <summary>
        /// Gets or sets the group display header text.
        /// </summary>
        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the header text displayed when the group is fully expanded.
        /// </summary>
        public string? ExpandedHeader
        {
            get => GetValue(ExpandedHeaderProperty);
            set => SetValue(ExpandedHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the group abstract icon resource or shape geometry.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the group container is currently expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the group container is pinned.
        /// </summary>
        public bool IsPinned
        {
            get => GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the pin management toggle is visible.
        /// </summary>
        public bool PinVisible
        {
            get => GetValue(PinVisibleProperty);
            set => SetValue(PinVisibleProperty, value);
        }

        /// <summary>
        /// Accessor for Attached Property GroupId.
        /// </summary>
        public static string GetGroupId(AvaloniaObject element) => element.GetValue(GroupIdProperty);

        /// <summary>
        /// Accessor for Attached Property GroupId.
        /// </summary>
        public static void SetGroupId(AvaloniaObject element, string value) => element.SetValue(GroupIdProperty, value);

        /// <inheritdoc />
        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var closeButton = e.NameScope.Find<Button>("PART_CloseButton");
            if (closeButton != null)
            {
                // Only collapse the group if it is NOT currently pinned
                closeButton.Click += (s, args) =>
                {
                    if (!IsPinned)
                    {
                        IsExpanded = false;
                    }
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
