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
    public class FtpAppUpdaterService : UpdaterServiceBase, IAppUpdaterService
    {
        private readonly FtpAppUpdaterOptions _ftpUpdaterOptions;
        private volatile ProgressInfo _filesProgress = new ProgressInfo();
        private bool isWindowsOS = true;
        private IProgress<FtpProgress> defaultProgress;
        public Action<double> ProgressHandler { get; set; }


        public FtpAppUpdaterService(ILogger<FtpAppUpdaterService> logger, FtpAppUpdaterOptions ftpAppUpdaterOptions): base(logger, ftpAppUpdaterOptions)
        {
            _ftpUpdaterOptions = ftpAppUpdaterOptions;
            defaultProgress = new Progress<FtpProgress>(onProgress);
            this.isWindowsOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public void Update(string version, Action onSuccess)
        {
            System.Threading.Thread.Sleep(1000);
            var cntxt = DownloadFilesAsync(version).Result;
            CompleteUpdateAsync(onSuccess).Wait();
        }

        public async Task UpdateAsync(string version, Action onSuccess)
        {
            await Delay(1000);

            var cntxt = await DownloadFilesAsync(version);

            await CompleteUpdateAsync(onSuccess);
        }


        void onProgress(FtpProgress x)
        {
            _filesProgress.Value = x.Progress;
            if (ProgressHandler != null)
                ProgressHandler(x.Progress);
        }

        public ProgressInfo ProgressInfo
        {
            get { return _filesProgress; }
            set
            {
                _filesProgress = value;
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
            client.ValidateCertificate += Client_ValidateCertificate;
            //client.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Ssl2;

            var ftpFolder = $"{_ftpUpdaterOptions.FtpFolderHostingTheApp}/{version}";
            var ftpFolderAndFile = $"{ftpFolder}/{filename}";
            //var lst = client.GetListing();
            return client.DownloadFileAsync(downloadPath, ftpFolderAndFile, FluentFTP.FtpLocalExists.Overwrite, FluentFTP.FtpVerify.Delete | FluentFTP.FtpVerify.Throw, defaultProgress);

        }

        public async Task<DownloadContext> DownloadFilesAsync(string version)
        {
            try
            {
                LogInfo(string.Format("..downloading to {0}", UpdatesDirPath));
                var apiDownloadPath = Path.Combine(UpdatesDirPath, _ftpUpdaterOptions.ArchiveName);
                _filesProgress = new ProgressInfo() { CurrentFile = _ftpUpdaterOptions.ArchiveName, CurrentFileIndex = 0, TotalFiles = _ftpUpdaterOptions.SecondaryFiles.Count + 1, Value = 0 };

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


        public async Task CompleteUpdateAsync(Action onSuccess)
        {
            //_logger.LogInformation("NeedsUpdate");
            LogInfo("NeedsUpdate");
            await Delay(3000);

            //System.Threading.Thread.Sleep(3000); //perimeno na einai sigouros oti sou ir8e to updating.html   
            var fileInfo = new FileInfo(typeof(FtpAppUpdaterService).Assembly.Location);

            //do update
            if (isWindowsOS)
            {
                using (var s = this.GetType().Assembly.GetManifestResourceStream("EgritosGroup.ConsoleClient.Resources.EgritosGroup.AspNetCore.Updater.zip"))
                {
                    ExtractFromStream(s, fileInfo.Directory.FullName);
                    //using (var archive = new ZipArchive(s))
                    //{
                    //    foreach (var entry in archive.Entries)
                    //    {
                    //        entry.ExtractToFile(Path.Combine(fileInfo.Directory.FullName, entry.Name), true);
                    //    }
                    //    //archive.ExtractToDirectory(fileInfo.Directory.FullName, true);
                    //}
                }
                var webconfig = Path.Combine(fileInfo.Directory.FullName, "web.config");
                if (File.Exists(webconfig))
                    _replaceNetCoreDllInWebConfig(webconfig, _ftpUpdaterOptions.AppEntryDll, "EgritosGroup.AspNetCore.Updater.dll");
            }
            else
            {
                //_logger.LogInformation("Linux Update Start copying files");
                LogInfo("Linux Update Start copying files");
                try
                {
                    DirectoryInfo appDi = fileInfo.Directory;
                    var settingsFile = Path.Combine(appDi.FullName, "UpdateSettings.json");
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(UpdateSettings));
                    UpdateSettings settings = null;
                    using (Stream stream = File.OpenRead(settingsFile))
                    {
                        settings = ser.ReadObject(stream) as UpdateSettings;
                    }
                    _CleanFiles(settings);

                    //Now Create all of the directories
                    foreach (string dirPath in Directory.GetDirectories(UnzipDirPath, "*", SearchOption.AllDirectories))
                        Directory.CreateDirectory(dirPath.Replace(UnzipDirPath, appDi.FullName));
                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(UnzipDirPath, "*.*", SearchOption.AllDirectories))
                        System.IO.File.Copy(newPath, newPath.Replace(UnzipDirPath, appDi.FullName), true);
                }
                catch (Exception ex)
                {
                    LogError("Linux Update Failed! \n Exception:{Exception} \n StackTrace: {StackTrace}", ex.GetAllMessages(), ex.StackTrace);
                    //_logger.LogError("Linux Update Failed! \n Exception:{Exception} \n StackTrace: {StackTrace}", ex.GetAllMessages(), ex.StackTrace);
                }

            }
            LogInfo("Program.Shutdown");
            //_logger.LogInformation("Program.Shutdown");
            onSuccess.Invoke();
            //Program.Shutdown();
        }

        private async Task DownloadSecondaryFiles(string version)
        {
            foreach (var item in _ftpUpdaterOptions.SecondaryFiles)
            {
                _filesProgress = new ProgressInfo() { CurrentFile = item.FileName, CurrentFileIndex = _filesProgress.CurrentFileIndex + 1, TotalFiles = _filesProgress.TotalFiles, Value = 0 };
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

        private void _CleanFiles(UpdateSettings settings)
        {
            var fileInfo = new FileInfo(typeof(FtpAppUpdaterService).Assembly.Location);
            DirectoryInfo appDi = fileInfo.Directory;
            //var updaterPath = UnzipDirPath;// Path.Combine(appDi.FullName, "updates", UnzipDirPath);
            string[] extensions = new[] { ".dll", ".pdb" };
            var filesLst = appDi.GetFiles()
                                 .Where(f => extensions.Contains(f.Extension.ToLower()))
                                 .ToArray();
            foreach (FileInfo file in filesLst)
            {
                file.Delete();
            }
            //string[] folders = { "clientSources", "refs", "runtimes", "Views", "wwwroot" };
            string[] foldersNotToDelete = { "updates", "logs" };
            if (settings?.ExcludedDirectoriesFromClean != null)
            {
                foldersNotToDelete = foldersNotToDelete.Concat(settings.ExcludedDirectoriesFromClean).Select(x => x.ToLower()).ToArray();
            }
            foreach (DirectoryInfo dir in appDi.GetDirectories())
            {
                if (!foldersNotToDelete.Contains(dir.Name.ToLower()))
                {
                    dir.Delete(true);
                }
            }
        }

        private void ForceDeleteFolder(string FolderName)
        {
            if (Directory.Exists(FolderName))
            {
                Directory.Delete(FolderName, recursive: true);
            }
        }

        private bool DownloadVersionItem(string version, string filename, string downloadPath)
        {
            var client = new FluentFTP.FtpClient(_ftpUpdaterOptions.Host, _ftpUpdaterOptions.Username, _ftpUpdaterOptions.Password);
            //FluentFTP.FtpVerify.Retry | FluentFTP.FtpVerify.Delete | FluentFTP.FtpVerify.Throw
            return client.DownloadFile(downloadPath, $"/{version}/{filename}", FluentFTP.FtpLocalExists.Overwrite, FluentFTP.FtpVerify.Delete | FluentFTP.FtpVerify.Throw);
        }

        private void _replaceNetCoreDllInWebConfig(string webconfig, string oldDll, string newDll)
        {
            string text = System.IO.File.ReadAllText(webconfig);
            text = text.Replace(oldDll, newDll);
            System.IO.File.WriteAllText(webconfig, text);
        }

        private Task Delay(int millis)
        {
            return Task.Delay(millis);
        }

        private static void Client_ValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }



    }
}
