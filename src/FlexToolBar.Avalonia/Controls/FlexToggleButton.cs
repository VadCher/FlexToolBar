using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives; // Location of standard ToggleButton
using Avalonia.Interactivity;
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents a specialized Ribbon toggle button that adapts its layout based on RibbonControlSize.
    /// Inherits all core state behaviors from the standard Avalonia ToggleButton.
    /// </summary>
    public class FlexToggleButton : ToggleButton
    {
        // 1. Icon Property
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<FlexToggleButton, object?>(nameof(Icon), null);

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // 2. Control Size Property
        public static readonly StyledProperty<RibbonControlSize> ControlSizeProperty =
            AvaloniaProperty.Register<FlexToggleButton, RibbonControlSize>(nameof(ControlSize), RibbonControlSize.Large);

        public RibbonControlSize ControlSize
        {
            get => GetValue(ControlSizeProperty);
            set => SetValue(ControlSizeProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ControlSizeProperty)
            {
                UpdateSizeStates(change.GetNewValue<RibbonControlSize>());
            }
            
            if (change.Property == ContentProperty)
            {
                ApplyAdaptiveToolTip();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            UpdateSizeStates(ControlSize);
        }

        private void UpdateSizeStates(RibbonControlSize size)
        {
            PseudoClasses.Set(":large", size == RibbonControlSize.Large);
            PseudoClasses.Set(":medium", size == RibbonControlSize.Medium);
            PseudoClasses.Set(":small", size == RibbonControlSize.Small);

            ApplyAdaptiveToolTip();
        }

        /// <summary>
        /// SMART TOOLTIP LIFE-CYCLE: Respects user-defined ToolTips and applies 
        /// the Content fallback only in Small layout modes.
        /// </summary>
        private void ApplyAdaptiveToolTip()
        {
            if (ControlSize != RibbonControlSize.Small) return;

            var currentTip = ToolTip.GetTip(this);
            if (currentTip != null) return;

            if (Content != null && (Content is string || Content is Control || Content is Visual))
            {
                ToolTip.SetTip(this, Content);
            }
        }
    }
}
