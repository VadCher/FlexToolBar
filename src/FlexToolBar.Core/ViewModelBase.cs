using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FlexToolBar.Core;

/// <summary>
/// Base class for all view models implementing the INotifyPropertyChanged interface
/// with an integrated reactive tracking engine.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    [JsonIgnore]
    public bool IsNew { get; init; } = false;
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property name.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the property value and reactively raises property mutations along with the global IsEdited state.
    /// Compatible natively with C# 13 'field' keyword semantics.
    /// </summary>
    protected virtual bool RaiseAndSetIfChanged<T>(ref T backingField, T newValue, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingField, newValue)) return false;

        backingField = newValue;

        OnPropertyChanged(propertyName);
        FlexLayoutManager.Instance.SetIsEdited();
        return true;
    }
}

