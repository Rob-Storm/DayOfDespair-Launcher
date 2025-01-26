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

        public static async Task<string> DownloadRelease(string owner, string repo, string release, string destinationFolder, IProgress<double> downloadProgress, IProgress<double> extractProgress)
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

                    await using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        //await urlResponse.Content.CopyToAsync(fs);

                        var contentStream = await urlResponse.Content.ReadAsStreamAsync();

                        var buffer = new byte[8192];
                        long totalRead = 0;
                        long totalLength = urlResponse.Content.Headers.ContentLength ?? 0;

                        int read;

                        while((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            totalRead += read;

                            downloadProgress.Report((double)totalRead / totalLength * 100);

                            await fs.WriteAsync(buffer, 0, read);
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
            //ZipFile.ExtractToDirectory(filePath, fileDirectory);

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
