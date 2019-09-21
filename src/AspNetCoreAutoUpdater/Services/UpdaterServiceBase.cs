using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AspNetCoreAutoUpdater.Extentions;

namespace AspNetCoreAutoUpdater
{
    public abstract class UpdaterServiceBase: IAppUpdaterService
    {
        readonly ILogger _logger;
        readonly AppUpdaterOptions _options;
        public OSPlatform CurrentOS { get; set; }
        public ProgressInfo ProgressInfo { get; set; }
        public string UnzipDirPath => Path.Combine(UpdatesDirPath, Path.GetFileNameWithoutExtension(_options.ArchiveName));

        public UpdaterServiceBase(ILogger logger, AppUpdaterOptions options)
        {
            _logger = logger;
            _options = options;

            var isWindowsOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            CurrentOS = isWindowsOS ? OSPlatform.Windows : OSPlatform.Linux;
        }


        public string UpdatesDirPath
        {
            get
            {
                var fullpath = Path.Combine(_options.ContentRootPath, "updates");
                Directory.CreateDirectory(fullpath);
                return fullpath;
            }
        }

        public abstract Task<DownloadContext> DownloadFilesAsync(string version);


        public void BeginUpdateProcess(string version, Action onSuccess)
        {
            BeginUpdateProcessAsync(version, onSuccess).GetAwaiter().GetResult();
        }

        public async Task BeginUpdateProcessAsync(string version, Action onSuccess)
        {
            await Task.Delay(1000);
            var cntxt = await DownloadFilesAsync(version);
            await CompleteUpdateAsync(onSuccess);
        }

        public void CreateSettingsFile()
        {
            var src = Path.Combine(UnzipDirPath, "UpdateSettings.json");
            var dest = Path.Combine(_options.ContentRootPath, "UpdateSettings.json");

            if (File.Exists(src))
            {
                LogDebug("Copying from {0} to {1}", src, dest);
                File.Copy(src, dest, true);
            }
            else
            {
                //onCreatingUpdateSettings();
                LogDebug("Creating UpdateSettings.json");
                var updateSettings = new UpdateSettings()
                {
                    AppEntryDll = _options.AppEntryDll,
                    DirectoryName = new DirectoryInfo(UnzipDirPath).Name,
                    ExcludedDirectoriesFromClean = new string[] { }
                };
                var s = Newtonsoft.Json.JsonConvert.SerializeObject(updateSettings);
                File.WriteAllText(dest, s);
            }
        }


        public async Task CompleteUpdateAsync(Action onSuccess)
        {
            //_logger.LogInformation("NeedsUpdate");
            LogInfo("NeedsUpdate");
            await Task.Delay(3000);

            //System.Threading.Thread.Sleep(3000); //perimeno na einai sigouros oti sou ir8e to updating.html   
            var fileInfo = new FileInfo(typeof(UpdaterServiceBase).Assembly.Location);

            //do update
            if (CurrentOS == OSPlatform.Windows)
            {
                using (var s = this.GetType().Assembly.GetManifestResourceStream("AspNetCoreAutoUpdater.Resources.AspNetCoreAutoUpdater.IISHandler.zip"))
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
                    _replaceNetCoreDllInWebConfig(webconfig, _options.AppEntryDll, "AspNetCoreAutoUpdater.IISHandler.dll");
            }
            else //OSPlatform.Linux
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



        public void LogDebug(string msg, params object[] items)
        {
            _logger.LogDebug(msg, items);
        }

        public void LogInfo(string msg)
        {
            _logger.LogInformation(msg);
        }

        public void LogError(string msg, params object[] items)
        {
            _logger.LogError(msg, items);
        }

        public void Unzip(string zipfile, string destDir)
        {
            ZipFile.ExtractToDirectory(zipfile, destDir);
        }

        public void ExtractFromStream(Stream s, string destDir)
        {
            using (var archive = new ZipArchive(s))
            {
                foreach (var entry in archive.Entries)
                    entry.ExtractToFile(Path.Combine(destDir, entry.Name), true);
            }
        }
        public void ForceDeleteFolder(string FolderName)
        {
            if (Directory.Exists(FolderName))
            {
                Directory.Delete(FolderName, recursive: true);
            }
        }



        private void _replaceNetCoreDllInWebConfig(string webconfig, string oldDll, string newDll)
        {
            string text = System.IO.File.ReadAllText(webconfig);
            text = text.Replace(oldDll, newDll);
            System.IO.File.WriteAllText(webconfig, text);
        }


        private void _CleanFiles(UpdateSettings settings)
        {
            var fileInfo = new FileInfo(typeof(UpdaterServiceBase).Assembly.Location);
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

    }
}
