using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;

namespace AspNetCoreAutoUpdater
{
    public abstract class UpdaterServiceBase
    {
        readonly ILogger _logger;
        readonly AppUpdaterOptions _options;
        public string UnzipDirPath => Path.Combine(UpdatesDirPath, Path.GetFileNameWithoutExtension(_options.ArchiveName));

        public UpdaterServiceBase(ILogger logger, AppUpdaterOptions options)
        {
            _logger = logger;
            _options = options;
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
    }
}
