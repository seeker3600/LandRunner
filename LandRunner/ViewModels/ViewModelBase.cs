using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LandRunner.ViewModels;

/// <summary>
/// MVVM—p‚ÌŠî–{ViewModel
/// </summary>
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
