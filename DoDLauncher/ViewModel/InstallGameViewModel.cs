using DoDLauncher.Model;
using DoDLauncher.MVVM;
using DoDLauncher.Util;

namespace DoDLauncher.ViewModel
{
    public class InstallGameViewModel : ViewModelBase
    {
		public event Action OnFinishInstall;

		private double _downloadProgress;
		public double DownloadProgress
		{
			get { return _downloadProgress; }
			set { _downloadProgress = value; OnPropertyChanged(); }
		}

		private long _downloadedBytes;

		public long DownloadedBytes
		{
			get { return _downloadedBytes; }
			set { _downloadedBytes = value; }
		}

		private long _totalBytes;

		public long TotalBytes
		{
			get { return _totalBytes; }
			set { _totalBytes = value; }
		}

		private double _downloadSpeed;

		public double DownloadSpeed
		{
			get { return _downloadSpeed; }
			set { _downloadSpeed = value; }
		}

		private double _extractProgress;

		public double ExtractProgress
		{
			get { return _extractProgress; }
			set { _extractProgress = value; OnPropertyChanged(); }
		}

		private GameInstance _instance;
		public GameInstance Instance
		{
			get { return _instance; }
			set { _instance = value; OnPropertyChanged(); }
		}

		public async Task StartDownload()
		{
            var downloadProgress = new Progress<(double percentage, long downloadedBytes, long totalBytes, double downloadSpeed)>(progress =>
            {
                DownloadProgress = progress.percentage;
                DownloadedBytes = progress.downloadedBytes;
                TotalBytes = progress.totalBytes;
                DownloadSpeed = progress.downloadSpeed / 1024 / 1024; // Convert to MB/s
            });

            var extractProgress = new Progress<double>(progress =>
            {
                ExtractProgress = progress;
            });

            Instance.ExecutablePath = await GitHubActions.DownloadRelease(
                "Rob-Storm",
                "DayOfDespair-Public",
                Instance.Version,
                Instance.Name,
                downloadProgress,
                extractProgress
            );
        }

	}
}
