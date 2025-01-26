using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;

namespace DoDLauncher.Util
{
    public class GitHubActions
    {
        public static async Task<string> GetLatestReleaseDescription(string owner, string repo)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "DoD-Launcher");

                try
                {
                    var response = await client.GetStringAsync(url);

                    JObject release = JObject.Parse(response);

                    return release["body"]?.ToString() ?? "Someone didn't add a description to the release. Hmmmmmmmm";
                }
                catch (Exception ex)
                {
                    return $"Error fecthing release info: {ex.Message}";
                }
            }
        }

        public static async Task<ObservableCollection<string>> GetAllReleases(string owner, string repo)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/tags";

            ObservableCollection<string> releases = new ObservableCollection<string>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "DoD-Launcher");

                try
                {
                    var response = await client.GetStringAsync(url);
                    JArray releaseArray = JArray.Parse(response);

                    foreach (var release in releaseArray)
                    {
                        string releaseName = release["name"]?.ToString();
                        if (!string.IsNullOrEmpty(releaseName))
                        {
                            releases.Add(releaseName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    releases.Add($"Error fecthing releases: {ex.Message}");
                }

                return releases;
            }
        }

        //boy this is a hefty method parameter list
        public static async Task<string> DownloadRelease(string owner, string repo, string release, string destinationFolder,
            IProgress<(double percentage, long downloadedBytes, long totalBytes, double downloadSpeed)> downloadProgress, 
            IProgress<double> extractProgress)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{release}";

            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "DoD-Launcher");

                    HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    string releaseJson = await response.Content.ReadAsStringAsync();

                    JObject releaseData = JObject.Parse(releaseJson);
                    JObject firstAsset = (JObject)releaseData["assets"][0];

                    string assetName = firstAsset["name"]?.ToString();
                    string downloadUrl = firstAsset["browser_download_url"]?.ToString();

                    HttpResponseMessage urlResponse = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    urlResponse.EnsureSuccessStatusCode();

                    string filePath = $@"Instances\{destinationFolder}\{assetName}";

                    var buffer = new byte[8192];
                    long downloadedBytes = 0;
                    long totalBytes = urlResponse.Content.Headers.ContentLength ?? 0;

                    await using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var contentStream = await urlResponse.Content.ReadAsStreamAsync();

                        DateTime lastTime = DateTime.Now;
                        long lastBytes = 0;

                        int read;

                        while((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            downloadedBytes += read;
                            await fs.WriteAsync(buffer, 0, read);

                            DateTime now = DateTime.Now;
                            double secondsElapsed = (now - lastTime).TotalSeconds;

                            if(secondsElapsed >= 0.5)
                            {
                                long bytesSinceLast = downloadedBytes - lastBytes;
                                double downloadSpeed = bytesSinceLast / secondsElapsed;

                                double percentage = (double)downloadedBytes / totalBytes * 100;

                                downloadProgress.Report((percentage, downloadedBytes, totalBytes, downloadSpeed));

                                lastTime = now;
                                lastBytes = downloadedBytes;
                            }
                        }
                    };

                    return await ExtractFolderRelease(filePath, assetName, extractProgress);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
        }

        private static async Task<string> ExtractFolderRelease(string filePath, string assetName, IProgress<double> extractProgress)
        {
            string fileDirectory = Path.GetDirectoryName(filePath);

            using (ZipArchive archive = ZipFile.OpenRead(filePath))
            {
                int totalFiles = archive.Entries.Count;
                int extractedFiles = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(fileDirectory, entry.FullName);

                    string destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if(!Directory.Exists(destinationDirectory))
                        Directory.CreateDirectory(destinationDirectory);

                    if(entry.Name != "")
                        await Task.Run(() => entry.ExtractToFile(destinationPath, overwrite: true));

                    extractedFiles++;

                    extractProgress.Report((double)extractedFiles / totalFiles * 100);
                }
            }

            File.Delete(filePath); // delete temporary zip folder to free up disk space on the users computer

            string[] exeFiles = Directory.GetFiles($@"{fileDirectory}\{assetName.Substring(0, assetName.Length - 4)}", "*.exe", SearchOption.TopDirectoryOnly);

            return exeFiles[0];
        }
    }
}
