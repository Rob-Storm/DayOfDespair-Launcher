using DoDLauncher.ViewModel;
using System.Windows;

namespace DoDLauncher.View
{
    /// <summary>
    /// Interaction logic for CreateInstanceWindow.xaml
    /// </summary>
    public partial class CreateInstanceWindow : Window
    {
        private CreateInstanceViewModel _viewModel;
        public CreateInstanceWindow(CreateInstanceViewModel viewmodel)
        {
            InitializeComponent();

            _viewModel = viewmodel;

            DataContext = _viewModel;

            _viewModel.OnCloseWindow += ViewModel_OnCloseWindow;
        }

        private void ViewModel_OnCloseWindow()
        {
            Close();
        }
    }
}
