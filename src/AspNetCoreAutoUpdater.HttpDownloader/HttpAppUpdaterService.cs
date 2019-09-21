using AspNetCoreAutoUpdater.Extentions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreAutoUpdater.HttpDownloader
{
    public class HttpAppUpdaterService : UpdaterServiceBase
    {
        private HttpAppUpdaterOptions _httpAppUpdaterOptions;

        public HttpAppUpdaterService(ILogger<HttpAppUpdaterService> logger, HttpAppUpdaterOptions httpAppUpdaterOptions) : base(logger, httpAppUpdaterOptions)
        {
            _httpAppUpdaterOptions = httpAppUpdaterOptions;
        }


        public override async Task<DownloadContext> DownloadFilesAsync(string version)
        {
            try
            {
                LogInfo(string.Format("..downloading to {0}", UpdatesDirPath));

                var destinationFilePath = Path.Combine(UpdatesDirPath, _httpAppUpdaterOptions.ArchiveName);
                ProgressInfo = new ProgressInfo() { CurrentFile = _httpAppUpdaterOptions.ArchiveName, CurrentFileIndex = 0, TotalFiles = _httpAppUpdaterOptions.SecondaryFiles.Count + 1, Value = 0 };

                await DownloadVersionItemAsync(version, _httpAppUpdaterOptions.ArchiveName, destinationFilePath);
                LogInfo(string.Format("downladed to {0}", destinationFilePath));
                ForceDeleteFolder(UnzipDirPath);
                LogInfo(string.Format("unzipping to {0}", UnzipDirPath));
                Unzip(destinationFilePath, UnzipDirPath);

                if (File.Exists(Path.Combine(UnzipDirPath, "web.config")))
                    File.Delete(Path.Combine(UnzipDirPath, "web.config"));

                await DownloadSecondaryFiles(version);

                if (!_httpAppUpdaterOptions.SkipUpdateSettingsFile)
                    CreateSettingsFile();

                return new DownloadContext();
            }
            catch (Exception e)
            {
                LogError(e.GetAllMessages() + e.StackTrace);
                //return new DownloadContext();
                //content = e.GetAllMessages() + e.StackTrace;
                throw new Exception("Error in Dowloading files from http.", e);
            }
        }

        private async Task DownloadVersionItemAsync(string version, string filename, string destinationFilePath)
        {
            var downloadFileUrl = new Uri(new Uri(_httpAppUpdaterOptions.HttpDownloadUrl), $"{version}/{filename}");

            using (var client = new HttpClientDownloadWithProgress(downloadFileUrl.ToString(), destinationFilePath, ProgressInfo))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => {
                    LogInfo($"DownloadVersionItem {progressPercentage}% ({totalBytesDownloaded}/{totalFileSize})");
                };

                await client.StartDownload();
            }

        }


        private async Task DownloadSecondaryFiles(string version)
        {
            foreach (var item in _httpAppUpdaterOptions.SecondaryFiles)
            {
                ProgressInfo = new ProgressInfo() { CurrentFile = item.FileName, CurrentFileIndex = ProgressInfo.CurrentFileIndex + 1, TotalFiles = ProgressInfo.TotalFiles, Value = 0 };
                //; ForceDeleteFolder(_settings.FixDllPath);
                ForceDeleteFolder(item.DownloadPath);

                Directory.CreateDirectory(item.DownloadPath);
                var fixdllDownloadPath = Path.Combine(item.DownloadPath, item.FileName);
                await DownloadVersionItemAsync(version, item.FileName, fixdllDownloadPath);

                if (item.Unzip)
                {
                    //ZipFile.ExtractToDirectory(fixdllDownloadPath, item.DownloadPath);
                    Unzip(fixdllDownloadPath, item.DownloadPath);
                    File.Delete(fixdllDownloadPath);
                }
            }

        }
    }
}
