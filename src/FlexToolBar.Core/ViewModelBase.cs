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
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets whether any reactive property value has been modified since the last state flush.
    /// </summary>
    [JsonIgnore]
    public bool IsEdited { get; private set; }

    /// <summary>
    /// Resets the modified state flag back to false after a successful persistence operation.
    /// </summary>
    public void ResetIsEdited()
    {
        if (!IsEdited) return;
        IsEdited = false;
        OnPropertyChanged(nameof(IsEdited));
    }

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
        IsEdited = true;

        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(IsEdited));

        return true;
    }
    protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

