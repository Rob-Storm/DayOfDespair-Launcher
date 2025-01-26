using DoDLauncher.Util;
using DoDLauncher.ViewModel;
using System.Windows;

namespace DoDLauncher.View
{
    /// <summary>
    /// Interaction logic for InstallGameWindow.xaml
    /// </summary>
    public partial class InstallGameWindow : Window
    {
        private InstallGameViewModel _viewModel;
        public InstallGameWindow(InstallGameViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            DataContext = _viewModel;

            _viewModel.OnFinishInstall += _viewModel_OnFinishInstall;

            Download();
        }

        private async void Download()
        {
            await _viewModel.StartDownload();
        }
        private void _viewModel_OnFinishInstall()
        {
            Close();
        }
    }
}
