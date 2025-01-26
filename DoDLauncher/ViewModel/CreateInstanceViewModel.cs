using DoDLauncher.Model;
using DoDLauncher.MVVM;
using DoDLauncher.Util;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace DoDLauncher.ViewModel
{
    public class CreateInstanceViewModel : ViewModelBase
    {
        //might be bad lol
        public event Action OnCloseWindow;

        public RelayCommand CreateInstanceCommand => new RelayCommand(execute => CreateInstance(), canExecute => IsNameValid() && !string.IsNullOrEmpty(Version));

        public bool EditingMode { get; set; } = false;
        public string InstanceName { get; set; } = string.Empty;
        public string Version { get; set; }

        private string _arguments;
        public string Arguments 
        { 
            get
            {
                return _arguments;
            }
            set
            {
                _arguments = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _releases;

        public ObservableCollection<string> Releases
        {
            get { return _releases; }
            set { _releases = value; OnPropertyChanged(); }
        }

        public string WindowLabel
        {
            get => EditingMode ? "Edit Instance" : "Create Instance";
            set => WindowLabel = value;
        }

        private GameInstance _instance;

        public GameInstance Instance
        {
            get { return _instance; }
            set { _instance = value; }
        }


        private MainViewModel _mainViewModel;

        public CreateInstanceViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            _releases = new ObservableCollection<string>();

            GetReleases();

        }

        private void CreateInstance()
        {
            ObservableCollection<string> arguments = new ObservableCollection<string>();
            string[] argLines = {string.Empty};

            if (Arguments != null) 
                argLines = Arguments.Split(new[] { '\n' }, StringSplitOptions.None);

            GameInstance instance = new GameInstance
            { 
                Name = InstanceName,
                Version = Version,
                Arguments = argLines,
            };

            if(EditingMode)
            {
                Instance = instance;
            }
            else
            {
                _mainViewModel.GameInstances.Add(instance);
            }

            OnCloseWindow?.Invoke();
            
        }

        public async Task GetReleases()
        {
            ObservableCollection<string> releases = await GitHubActions.GetAllReleases("Rob-Storm", "DayOfDespair-Public");

            Application.Current.Dispatcher.Invoke(() =>
            {
                Releases = releases;
            });
        }

        private bool IsNameValid()
        {

            return !string.IsNullOrWhiteSpace(InstanceName);
        }
    }
}
