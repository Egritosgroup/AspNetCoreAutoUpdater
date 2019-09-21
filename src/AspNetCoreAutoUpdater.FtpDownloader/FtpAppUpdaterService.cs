using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AspNetCoreAutoUpdater.Extentions;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace AspNetCoreAutoUpdater.FtpDownloader
{
    public class FtpAppUpdaterService : UpdaterServiceBase
    {
        private readonly FtpAppUpdaterOptions _ftpUpdaterOptions;
        private IProgress<FtpProgress> defaultProgress;
        public Action<double> ProgressHandler { get; set; }

        public FtpAppUpdaterService(ILogger<FtpAppUpdaterService> logger, FtpAppUpdaterOptions ftpAppUpdaterOptions): base(logger, ftpAppUpdaterOptions)
        {
            _ftpUpdaterOptions = ftpAppUpdaterOptions;
            defaultProgress = new Progress<FtpProgress>(onProgress);
        }


        void onProgress(FtpProgress x)
        {
            ProgressInfo.Value = x.Progress;
            if (ProgressHandler != null)
                ProgressHandler(x.Progress);
        }


        public override async Task<DownloadContext> DownloadFilesAsync(string version)
        {
            try
            {
                LogInfo(string.Format("..downloading to {0}", UpdatesDirPath));
                var apiDownloadPath = Path.Combine(UpdatesDirPath, _ftpUpdaterOptions.ArchiveName);
                ProgressInfo = new ProgressInfo() { CurrentFile = _ftpUpdaterOptions.ArchiveName, CurrentFileIndex = 0, TotalFiles = _ftpUpdaterOptions.SecondaryFiles.Count + 1, Value = 0 };

                await DownloadVersionItemAsync(version, _ftpUpdaterOptions.ArchiveName, apiDownloadPath);
                LogInfo(string.Format("downladed to {0}", apiDownloadPath));
                ForceDeleteFolder(UnzipDirPath);
                LogInfo(string.Format("unzipping to {0}", UnzipDirPath));
                Unzip(apiDownloadPath, UnzipDirPath);

                if (File.Exists(Path.Combine(UnzipDirPath, "web.config")))
                    File.Delete(Path.Combine(UnzipDirPath, "web.config"));

                await DownloadSecondaryFiles(version);

                if (!_ftpUpdaterOptions.SkipUpdateSettingsFile)
                    CreateSettingsFile();

                return new DownloadContext();
            }
            catch (Exception e)
            {
                LogError(e.GetAllMessages() + e.StackTrace);
                //return new DownloadContext();
                //content = e.GetAllMessages() + e.StackTrace;
                throw new Exception("Error in Dowloading files from ftp.",e);
            }
        }



        private Task DownloadVersionItemAsync(string version, string filename, string downloadPath)
        {
            FluentFTP.FtpTrace.LogToFile = "log_FluentFTP_actions.logs";

            FluentFTP.FtpTrace.LogUserName = false;   // hide FTP user names
            FluentFTP.FtpTrace.LogPassword = false;   // hide FTP passwords
            FluentFTP.FtpTrace.LogIP = true; 	// hide FTP server IP addresses

            //var client = new FluentFTP.FtpClient(_ftpUpdaterOptions.Host, _ftpUpdaterOptions.Username, _ftpUpdaterOptions.Password);
            //FluentFTP.FtpVerify.Retry | FluentFTP.FtpVerify.Delete | FluentFTP.FtpVerify.Throw
            var client = new FluentFTP.FtpClient(_ftpUpdaterOptions.Host, _ftpUpdaterOptions.Username, _ftpUpdaterOptions.Password);
            client.EncryptionMode = FtpEncryptionMode.None;

            if(_ftpUpdaterOptions.TrustClientCertificate)
                client.ValidateCertificate += Client_ValidateCertificate;
            //client.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Ssl2;

            var ftpFolder = $"{_ftpUpdaterOptions.FtpFolderHostingTheApp}/{version}";
            var ftpFolderAndFile = $"{ftpFolder}/{filename}";
            //var lst = client.GetListing();
            return client.DownloadFileAsync(downloadPath, ftpFolderAndFile, FluentFTP.FtpLocalExists.Overwrite, FluentFTP.FtpVerify.Delete | FluentFTP.FtpVerify.Throw, defaultProgress);
        }

        private async Task DownloadSecondaryFiles(string version)
        {
            foreach (var item in _ftpUpdaterOptions.SecondaryFiles)
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



        private static void Client_ValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }

    }
}
