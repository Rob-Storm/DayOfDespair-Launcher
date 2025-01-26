using DoDLauncher.Model;
using DoDLauncher.MVVM;
using DoDLauncher.Util;
using DoDLauncher.View;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace DoDLauncher.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            _gameInstances = new ObservableCollection<GameInstance>();

            GetLatestReleaseNotes();

            if (File.Exists("instances.json"))
            {
                LoadInstanceJson();
            }

            GameInstances.CollectionChanged += GameInstances_CollectionChanged;

            ShowNoInstanceWarning();

            ShowInstanceValidationMessages();
        }
        private void GameInstances_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WriteInstanceJson();
        }

        #region Button Commands

        public RelayCommand StartInstanceCommand => new RelayCommand(execute => StartInstance(), canExecute => SelectedInstance != null);
        public RelayCommand CreateInstanceCommand => new RelayCommand(execute => AddInstance());
        public RelayCommand EditInstanceCommand => new RelayCommand(execute => EditInstance(), canExecute => SelectedInstance != null);
        public RelayCommand RemoveInstanceCommand => new RelayCommand(execute => RemoveInstance(), canExecute => SelectedInstance != null);

        #endregion

        #region Fields

        private string _releaseNotes;
        public string ReleaseNotes
        {
            get
            {
                return _releaseNotes;
            }
            set
            {
                _releaseNotes = value;
                OnPropertyChanged();
            }
        }


        private GameInstance _selectedInstances;
        public GameInstance SelectedInstance
        {
            get
            {
                return _selectedInstances;
            }

            set
            {
                _selectedInstances = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<GameInstance> _gameInstances;
        public ObservableCollection<GameInstance> GameInstances
        {
            get
            {
                return _gameInstances;
            }

            set
            {
                _gameInstances = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Command Implementations

        private async void StartInstance()
        {
            if(!SelectedInstance.Installed)
            {
                InstallGameViewModel installVM = new InstallGameViewModel { Instance = SelectedInstance };
                InstallGameWindow installWindow = new InstallGameWindow(installVM);

                installWindow.ShowDialog();

                GameInstances[GameInstances.IndexOf(SelectedInstance)].Installed = true;
                WriteInstanceJson();
            }

            ProcessStartInfo gameSettings = new ProcessStartInfo();
            gameSettings.FileName = SelectedInstance.ExecutablePath;

            using(Process proc = Process.Start(gameSettings))
            {
                proc.WaitForExit();

                MessageBox.Show($"Day of Despair exited with code: {proc.ExitCode}");
            }

        }

        private void AddInstance()
        {
            ShowInstanceWindow(false);
        }

        private void EditInstance()
        {
            ShowInstanceWindow(true);
        }
        private void RemoveInstance()
        {
            GameInstance instance = SelectedInstance;
            SelectedInstance = null;

            GameInstances.Remove(instance);

            Directory.Delete($@"Instances/{instance.Name}", true);
        }

        #endregion

        #region Misc Methods

        public void ShowInstanceWindow(bool editing)
        {
            int instanceCount = GameInstances.Count;

            CreateInstanceViewModel instanceVM = new CreateInstanceViewModel(this);
            CreateInstanceWindow instanceWindow = new CreateInstanceWindow(instanceVM);

            if (editing)
            {
                instanceVM.InstanceName = SelectedInstance.Name;
                instanceVM.Version = SelectedInstance.Version;

                string arguments = string.Empty;

                foreach (string arg in SelectedInstance.Arguments)
                {
                    arguments += $"{arg}\n";
                }

                instanceVM.Arguments = arguments;
                instanceVM.Instance = SelectedInstance;
            }

            instanceVM.EditingMode = editing;

            instanceWindow.ShowDialog();
            if(instanceCount != GameInstances.Count)
                Directory.CreateDirectory(@$"Instances\{GameInstances.LastOrDefault().Name}");
        }

        public async Task GetLatestReleaseNotes()
        {
            ReleaseNotes = "Fecthing latest release notes...";

            string description = await GitHubActions.GetLatestReleaseDescription("Rob-Storm", "DayOfDespair-Public");

            Application.Current.Dispatcher.Invoke(() =>
            {
                ReleaseNotes = description;
            });
        }

        public void WriteInstanceJson()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };

            string instanceContents = JsonConvert.SerializeObject(GameInstances, settings);

            File.WriteAllTextAsync($"instances.json", instanceContents);
        }

        public void LoadInstanceJson()
        {
            string jsonContents = File.ReadAllText("instances.json");

            GameInstances = JsonConvert.DeserializeObject<ObservableCollection<GameInstance>>(jsonContents);
        }

        public async void ShowNoInstanceWarning()
        {
            await Task.Delay(100);

            if (!File.Exists("instances.json") || GameInstances == null || GameInstances.Count <= 0)
            {
                MessageBox.Show("No instances have been detected by the launcher.\n\nIn order to play Day of Despair, you will need to create an instance either by right clicking the list or using the 'Create New Instance' button.",
                    "No instances found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public InstanceChanges ValidateInstances()
        {
            bool removedInstances = false;
            bool changedInstalledState = false;

            List<GameInstance> removeInstances = new List<GameInstance>();

            foreach(var instance in GameInstances)
            {
                if(Path.Exists($@"Instances/{instance.Name}"))
                {
                    if (!Path.Exists(instance.ExecutablePath))
                    {
                        instance.Installed = false;
                        changedInstalledState = true;
                    }
                }
                else
                {
                    removeInstances.Add(instance);
                    removedInstances = true;
                }
            }

            if(removeInstances.Count > 0)
            {
                foreach (GameInstance instance in removeInstances)
                {
                    GameInstances.Remove(instance);
                }
            }

            return new InstanceChanges { Removed = removedInstances, Changed = changedInstalledState};
        }

        public async void ShowInstanceValidationMessages()
        {
            InstanceChanges changes = ValidateInstances();
            if (changes.Removed)
            {
                await Task.Delay(100);
                MessageBox.Show("One or more instances have been removed due to missing instance folders.", "Instance Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (changes.Changed)
            {
                await Task.Delay(100);
                MessageBox.Show("Could not find installation path for one or more instances.\n\nYou must redownload to play the affected instances",
                    "Instance Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        public struct InstanceChanges
        {
            public bool Removed;
            public bool Changed;
        }

        #endregion
    }
}
