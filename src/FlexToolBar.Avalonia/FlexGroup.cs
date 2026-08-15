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
        public FlexGroupViewModel GroupViewModel { get; private set; } = new();

        protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _xamlDefaultIsExpanded = IsExpanded;
            _xamlDefaultIsPinned = IsPinned;

            _isRegistered = true;
            UpdatePseudoClasses();
        }
        public void BindToCoreModel(FlexGroupViewModel coreModel)
        {
            if (coreModel.IsNew)
            {
                coreModel.IsExpanded = GroupViewModel.IsExpanded;
                coreModel.IsPinned = GroupViewModel.IsPinned;
            }
            GroupViewModel = coreModel;

            GroupViewModel.Header = this.Header;
            GroupViewModel.ExpandedHeader = this.ExpandedHeader;
            GroupViewModel.Icon = this.Icon;
            GroupViewModel.PinVisible = this.PinVisible;

            this.Bind(IsExpandedProperty, new Binding(nameof(GroupViewModel.IsExpanded)) { Source = GroupViewModel, Mode = BindingMode.TwoWay });
            this.Bind(IsPinnedProperty, new Binding(nameof(GroupViewModel.IsPinned)) { Source = GroupViewModel, Mode = BindingMode.TwoWay });

            UpdatePseudoClasses();
        }

        private void SyncUiToCore(string propertyName, object? newValue)
        {
            if (!_isRegistered) return;

            if (propertyName == nameof(FlexGroupViewModel.IsExpanded)) GroupViewModel.IsExpanded = (bool)(newValue ?? true);
            else if (propertyName == nameof(FlexGroupViewModel.IsPinned)) GroupViewModel.IsPinned = (bool)(newValue ?? false);
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
