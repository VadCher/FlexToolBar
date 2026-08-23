using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia
{
    public class FlexGroup : ContentControl
    {
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
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(IsExpanded), true);

        public static readonly StyledProperty<bool> IsPinnedProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(IsPinned), false);

        public static readonly StyledProperty<bool> PinVisibleProperty =
            AvaloniaProperty.Register<FlexGroup, bool>(nameof(PinVisible), true);

        static FlexGroup()
        {
            // UI -> Core Direction: Intercepts active layout modifications and marshals them to the Core model layer
            IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => { if (x.GroupViewModel is not null && x._isRegistered) x.GroupViewModel.IsExpanded = e.GetNewValue<bool>(); });
            IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => { if (x.GroupViewModel is not null && x._isRegistered) x.GroupViewModel.IsPinned = e.GetNewValue<bool>(); });

            // Visual State Management: Triggers pseudo-class synchronization routines instantly upon state shifts
            IsExpandedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.UpdatePseudoClasses());
            IsPinnedProperty.Changed.AddClassHandler<FlexGroup>((x, e) => x.UpdatePseudoClasses());
        }

        public FlexGroup()
        {
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

        public FlexGroupViewModel GroupViewModel { get; private set; } = new();

        protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _xamlDefaultIsExpanded = IsExpanded;
            _xamlDefaultIsPinned = IsPinned;
            _isRegistered = true;
            UpdatePseudoClasses();
        }
        // Clean Hydration Engine & Core/UI Communication Bridge Wire-up
        public void BindToCoreModel(FlexGroupViewModel coreModel)
        {
            if (GroupViewModel != null) GroupViewModel.PropertyChanged -= OnCoreModelPropertyChanged;

            // Cold Start Pipeline: Fresh runtime core model absorbs unique inline declarative XAML default specifications
            if (coreModel.IsNew)
            {
                coreModel.IsExpanded = _xamlDefaultIsExpanded;
                coreModel.IsPinned = _xamlDefaultIsPinned;
            }

            GroupViewModel = coreModel;

            if (!GroupViewModel.IsNew)
            {
                SetCurrentValue(IsExpandedProperty, GroupViewModel.IsExpanded);
                SetCurrentValue(IsPinnedProperty, GroupViewModel.IsPinned);
            }

            GroupViewModel.PropertyChanged += OnCoreModelPropertyChanged;

            UpdatePseudoClasses();
        }

        // Core -> UI Direction: Translates internal core state alterations to the layout engine safely preserving custom developer bindings
        private void OnCoreModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (GroupViewModel == null) return;

            if (args.PropertyName == nameof(FlexGroupViewModel.IsExpanded))
            {
                SetCurrentValue(IsExpandedProperty, GroupViewModel.IsExpanded);
            }
            else if (args.PropertyName == nameof(FlexGroupViewModel.IsPinned))
            {
                SetCurrentValue(IsPinnedProperty, GroupViewModel.IsPinned);
            }
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

        // Final Lifecycle Cleanup Step
        protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
        {
            if (GroupViewModel != null) GroupViewModel.PropertyChanged -= OnCoreModelPropertyChanged;
            base.OnDetachedFromVisualTree(e);
        }
    }
}
