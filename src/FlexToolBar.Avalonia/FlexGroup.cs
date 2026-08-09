using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia
{
    /// <summary>
    /// Represents a smart responsive container control switching between collapsed and expanded states.
    /// Fully preserves and cascades dynamic user-defined external XAML DataContext bindings.
    /// </summary>
    public class FlexGroup : ContentControl
    {
    // protected override Type StyleKeyOverride => typeof(FlexGroup);
    public FlexGroup()
    {
    }
        private bool _xamlDefaultIsExpanded = true;
        private bool _xamlDefaultIsPinned = false;
        private bool _isRegistered;

        public static readonly StyledProperty<global::Avalonia.Markup.Xaml.Templates.ControlTemplate?> SeparatorTemplateProperty =
            AvaloniaProperty.Register<FlexGroup, global::Avalonia.Markup.Xaml.Templates.ControlTemplate?>(nameof(SeparatorTemplate), null);

        public static readonly StyledProperty<string> GroupIdProperty =
            AvaloniaProperty.Register<FlexGroup, string>(nameof(GroupId), string.Empty);

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
            AvaloniaProperty.Register<FlexGroup, bool>(
                nameof(IsPinned), 
                false,
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> PinVisibleProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(PinVisible), true);

        static FlexGroup()
        {
            // FocusableProperty.OverrideMetadata<FlexGroup>(
            //     new StyledPropertyMetadata<bool>(true));
            IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.SyncUiToCore(nameof(FlexGroupViewModel.IsExpanded), e.GetNewValue<bool>()));
            IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.SyncUiToCore(nameof(FlexGroupViewModel.IsPinned), e.GetNewValue<bool>()));
            
            IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.UpdatePseudoClasses());
            IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.UpdatePseudoClasses());
        }

        public global::Avalonia.Markup.Xaml.Templates.ControlTemplate? SeparatorTemplate
        {
            get => GetValue(SeparatorTemplateProperty);
            set => SetValue(SeparatorTemplateProperty, value);
        }

        public string GroupId
        {
            get => GetValue(GroupIdProperty);
            set => SetValue(GroupIdProperty, value);
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

        public bool XamlDefaultIsExpanded => _xamlDefaultIsExpanded;
        public bool XamlDefaultIsPinned => _xamlDefaultIsPinned;

        /// <summary>
        /// Extensible core view model state instance supporting clean external init pipelines.
        /// </summary>
        public FlexGroupViewModel GroupViewModel { get; init; } = new();

        protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // Cache static compile-time developer configurations safely on boot phase
            _xamlDefaultIsExpanded = IsExpanded;
            _xamlDefaultIsPinned = IsPinned;

            // Seed initial UI values into the core model container
            GroupViewModel.Header = Header;
            GroupViewModel.ExpandedHeader = ExpandedHeader;
            GroupViewModel.Icon = Icon;
            GroupViewModel.PinVisible = PinVisible;
            GroupViewModel.IsExpanded = IsExpanded;
            GroupViewModel.IsPinned = IsPinned;

            // REACTIVE BRIDGE (CORE -> UI): Listen to model updates (like JSON loading) and update UI controls safely
            GroupViewModel.PropertyChanged += OnCoreModelPropertyChanged;

            _isRegistered = true;
            UpdatePseudoClasses();
        }

        private void SyncUiToCore(string propertyName, object? newValue)
        {
            if (!_isRegistered) return;

            if (propertyName == nameof(FlexGroupViewModel.IsExpanded)) GroupViewModel.IsExpanded = (bool)(newValue ?? true);
            else if (propertyName == nameof(FlexGroupViewModel.IsPinned)) GroupViewModel.IsPinned = (bool)(newValue ?? false);
        }

        private void OnCoreModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Using direct SetValue ensures we safely notify and cascade values into external user TwoWay XAML bindings
            if (e.PropertyName == nameof(FlexGroupViewModel.IsExpanded)) SetValue(IsExpandedProperty, GroupViewModel.IsExpanded);
            else if (e.PropertyName == nameof(FlexGroupViewModel.IsPinned)) SetValue(IsPinnedProperty, GroupViewModel.IsPinned);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            var pinButton = e.NameScope.Find<Button>("PART_PinButton");
            if (pinButton != null) pinButton.Click += (s, args) => { IsPinned = !IsPinned; };

            var closeButton = e.NameScope.Find<Button>("PART_CloseButton");
            if (closeButton != null)
            {
                closeButton.Click += (s, args) => { if (!IsPinned) IsExpanded = false; };
            }

            var collapsedButton = e.NameScope.Find<Button>("PART_CollapsedButton");
            if (collapsedButton != null)
            {
                collapsedButton.Click += (s, args) => IsExpanded = true;
            }

            UpdatePseudoClasses();
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":expanded", IsExpanded);
            PseudoClasses.Set(":collapsed", !IsExpanded);
            PseudoClasses.Set(":pinned", IsPinned);
        }
    }
}
