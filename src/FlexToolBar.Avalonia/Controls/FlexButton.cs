using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity; // FIX: Location of VisualTreeAttachmentEventArgs in Avalonia 12
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents a specialized Ribbon button that adapts its layout based on RibbonControlSize.
    /// Inherits all core behaviors from the standard Avalonia Button.
    /// </summary>
    public class FlexButton : Button
    {
        // 1. Icon Property: Can host text glyphs, Vector Paths, or images
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<FlexButton, object?>(nameof(Icon), null);

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // 2. Control Size Property: Drives the layout template switching
        public static readonly StyledProperty<RibbonControlSize> ControlSizeProperty =
            AvaloniaProperty.Register<FlexButton, RibbonControlSize>(nameof(ControlSize), RibbonControlSize.Large);

        public RibbonControlSize ControlSize
        {
            get => GetValue(ControlSizeProperty);
            set => SetValue(ControlSizeProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // Dynamically manage pseudo-classes for XAML styling hooks based on size mutations
            if (change.Property == ControlSizeProperty)
            {
                UpdateSizeStates(change.GetNewValue<RibbonControlSize>());
            }
            
            // If the Content (text label) changes in runtime, re-evaluate our adaptive ToolTip fallback
            if (change.Property == ContentProperty)
            {
                ApplyAdaptiveToolTip();
            }
        }

        // FIX: Using the correct Avalonia 12 override with standard interactivity arguments
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // Set initial pseudo-class and ToolTip states on visual boot phase
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
        /// SMART TOOLTIP LIFE-CYCLE: Only applies a fallback string if the developer 
        /// hasn't already explicitly specified a rich custom ToolTip in XAML.
        /// </summary>
        private void ApplyAdaptiveToolTip()
        {
            // If we are NOT in Small mode, we don't force any fallback text restrictions
            if (ControlSize != RibbonControlSize.Small) return;

            // Check if a tool tip is already defined on this instance hierarchy
            var currentTip = ToolTip.GetTip(this);
            
            // VADIM'S RULE: If a custom tip is already present, do NOT overwrite it!
            if (currentTip != null) return;

            // Fallback: If Content is a clean readable string/text, gracefully extract it as the default tip
            // FIX: Substituted old IControl with the modern base Control type
            if (Content != null && (Content is string || Content is Control || Content is Visual))
            {
                ToolTip.SetTip(this, Content);
            }
        }
    }
}
