using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlexToolBar.Avalonia.Demo.ViewModels
{
    /// <summary>
    /// Main view model for the FlexToolBar demonstration application.
    /// Handles only global interactivity toggles for the demo UI.
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isSingleExpandMode = false;

        /// <summary>
        /// Command executed by the UI Reset button.
        /// In a real app, this would clear the JSON state file.
        /// </summary>
        [RelayCommand]
        private void ResetDemoLayout()
        {
            // Logic to simulate layout reset
            IsSingleExpandMode = false;
        }
    }
}
