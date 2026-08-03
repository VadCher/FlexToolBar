using CommunityToolkit.Mvvm.ComponentModel;

namespace FlexToolBar.Avalonia.Demo.ViewModels
{
    /// <summary>
    /// Pure, clean view model holding only interactive properties bound via XAML.
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isSingleExpandMode = false;
    }
}
