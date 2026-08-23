using Avalonia.Controls;
using FlexToolBar.Core;

namespace FlexToolBar.Avalonia.Demo.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.axaml.
    /// Pure presentation class decoupled from layout persistence operations.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            FlexLayoutManager.GetToolBar("SecondToolBar").TabStripVisible = false;
            InitializeComponent();
            // SecondToolBar?.ViewModel?.TabStripVisible = false;
        }
    }
}
