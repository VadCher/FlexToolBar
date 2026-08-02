using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace FlexToolBar.Core;

/// <summary>
/// Represents a simple command implementation for ICommand.
/// </summary>
internal class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the RelayCommand class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

#pragma warning disable CS0067
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();
}

/// <summary>
/// Represents the implementation of the IFlexToolBarViewModel interface.
/// </summary>
public class FlexToolBarViewModel : ViewModelBase, IFlexToolBarViewModel
{
    private bool _isSingleExpandGroup;
    private readonly ObservableCollection<IFlexTabViewModel> _tabs = new();

    /// <summary>
    /// Initializes a new instance of the FlexToolBarViewModel class.
    /// </summary>
    public FlexToolBarViewModel()
    {
        Tabs.CollectionChanged += OnTabsCollectionChanged;
        ResetLayoutCommand = new RelayCommand(ResetLayout);
    }

    /// <inheritdoc />
    public bool IsSingleExpandGroup
    {
        get => _isSingleExpandGroup;
        set => SetProperty(ref _isSingleExpandGroup, value);
    }

    /// <inheritdoc />
    public ObservableCollection<IFlexTabViewModel> Tabs => _tabs;

    /// <inheritdoc />
    public ICommand ResetLayoutCommand { get; }

    private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var tab in e.NewItems.OfType<IFlexTabViewModel>())
            {
                HookTabGroups(tab);
            }
        }

        if (e.OldItems != null)
        {
            foreach (var tab in e.OldItems.OfType<IFlexTabViewModel>())
            {
                UnhookTabGroups(tab);
            }
        }
    }

    private void HookTabGroups(IFlexTabViewModel tab)
    {
        tab.Groups.CollectionChanged += OnGroupsCollectionChanged;
        foreach (var group in tab.Groups)
        {
            if (group is INotifyPropertyChanged notifyGroup)
            {
                notifyGroup.PropertyChanged += OnGroupPropertyChanged;
            }
        }
    }

    private void UnhookTabGroups(IFlexTabViewModel tab)
    {
        tab.Groups.CollectionChanged -= OnGroupsCollectionChanged;
        foreach (var group in tab.Groups)
        {
            if (group is INotifyPropertyChanged notifyGroup)
            {
                notifyGroup.PropertyChanged -= OnGroupPropertyChanged;
            }
        }
    }

    private void OnGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var group in e.NewItems.OfType<IFlexGroupViewModel>())
            {
                if (group is INotifyPropertyChanged notifyGroup)
                {
                    notifyGroup.PropertyChanged += OnGroupPropertyChanged;
                }
            }
        }

        if (e.OldItems != null)
        {
            foreach (var group in e.OldItems.OfType<IFlexGroupViewModel>())
            {
                if (group is INotifyPropertyChanged notifyGroup)
                {
                    notifyGroup.PropertyChanged -= OnGroupPropertyChanged;
                }
            }
        }
    }

    private void OnGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!IsSingleExpandGroup) return;
        if (e.PropertyName == nameof(IFlexGroupViewModel.IsExpanded) && sender is IFlexGroupViewModel changedGroup)
        {
            if (changedGroup.IsExpanded)
            {
                foreach (var tab in Tabs)
                {
                    if (tab.Groups.Contains(changedGroup))
                    {
                        foreach (var group in tab.Groups)
                        {
                            if (group != changedGroup && !group.IsPinned && group.IsExpanded)
                            {
                                group.IsExpanded = false;
                            }
                        }
                        break;
                    }
                }
            }
        }
    }

    private void ResetLayout()
    {
        foreach (var tab in Tabs)
        {
            foreach (var group in tab.Groups)
            {
                group.IsExpanded = true;
                group.IsPinned = false;
            }
        }
    }
}
