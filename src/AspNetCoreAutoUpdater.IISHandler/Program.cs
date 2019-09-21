using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AspNetCoreAutoUpdater.IISHandler
{
    class Program
    {
        private static string pathLogFile;
        private static DirectoryInfo di;

        static void Main(string[] args)
        {
            di = new DirectoryInfo(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            //di = new DirectoryInfo(@"\\egs02\wwwroot\kliseis.demo.egritosgroup.gr"); gia test
            pathLogFile = Path.Combine(di.FullName, @"Updater.log");

            AppendInLog(pathLogFile, $"-----------EgritosGroup.AspNetCore.Updater Started ----{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}");
            AppendInLog(pathLogFile, $"-----------EgritosGroup.AspNetCore.Updater Started 2 ----{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}");

            var settingsFile = Path.Combine(di.FullName, "UpdateSettings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    AppendInLog(pathLogFile, $"-----------Updating using settings ({settingsFile}) ----{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}");
                    //var s = File.ReadAllText(settingsFile);
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(UpdateSettings)); //.JsonReaderWriterFactory.CreateJsonReader(File.OpenRead(settingsFile), new System.Xml.XmlDictionaryReaderQuotas() )
                    UpdateSettings settings = null;
                    using (Stream stream = File.OpenRead(settingsFile))
                    {
                        settings = ser.ReadObject(stream) as UpdateSettings;
                    }
                    //var settings = JsonConvert.DeserializeObject<UpdateSettings>(s);
                    UpdateBySettings(settings);
                    return;
                }
                catch (Exception ex)
                {
                    AppendInLog(pathLogFile, $"-----------Failed updating by settings ({ex.StackTrace}) ----{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}");
                }
            }

            try
            {
                if (FileExists(di.FullName, "EgritosGroup.Auth.Server.dll"))
                {
                    UpdateBySettings(new UpdateSettings() { AppEntryDll = "EgritosGroup.Auth.Server.dll", DirectoryName = Path.Combine(di.FullName, "updates", "EgritosGroup.Auth.Server") });

                    //var updaterPath = Path.Combine(di.FullName, "updates", "EgritosGroup.Auth.Server");
                    ////Now Create all of the directories
                    //foreach (string dirPath in Directory.GetDirectories(updaterPath, "*", SearchOption.AllDirectories))
                    //    Directory.CreateDirectory(dirPath.Replace(updaterPath, di.FullName));
                    ////Copy all the files & Replaces any files with the same name
                    //foreach (string newPath in Directory.GetFiles(updaterPath, "*.*", SearchOption.AllDirectories))
                    //    File.Copy(newPath, newPath.Replace(updaterPath, di.FullName), true);

                    //var webconfig = Path.Combine(di.FullName, "web.config");
                    //_replaceNetCoreDllInWebConfig(webconfig, "EgritosGroup.AspNetCore.Updater.dll", "EgritosGroup.Auth.Server.dll");
                }
                else if (FileExists(di.FullName, "EgritosGroup.Finance.Core.HttpServer.dll"))
                {
                    UpdateBySettings(new UpdateSettings() { AppEntryDll = "EgritosGroup.Finance.Core.HttpServer.dll", DirectoryName = Path.Combine(di.FullName, "updates", "EgritosGroup.Finance.Core.HttpServer") });
                    //    var updaterPath = Path.Combine(di.FullName, "updates", "EgritosGroup.Finance.Api");
                    //    //Now Create all of the directories
                    //    foreach (string dirPath in Directory.GetDirectories(updaterPath, "*", SearchOption.AllDirectories))
                    //        Directory.CreateDirectory(dirPath.Replace(updaterPath, di.FullName));
                    //    //Copy all the files & Replaces any files with the same name
                    //    foreach (string newPath in Directory.GetFiles(updaterPath, "*.*", SearchOption.AllDirectories))
                    //        try
                    //        {
                    //            File.Copy(newPath, newPath.Replace(updaterPath, di.FullName), true);
                    //        }
                    //        catch (Exception ex1)
                    //        {
                    //            AppendInLog(pathLogFile, $"Cannot copy file {newPath} {ex1.Message}");
                    //            AppendInLog(pathLogFile, ex1.StackTrace);
                    //        }


                    //    var webconfig = Path.Combine(di.FullName, "web.config");
                    //    _replaceNetCoreDllInWebConfig(webconfig, "EgritosGroup.AspNetCore.Updater.dll", "EgritosGroup.Finance.Core.HttpServer.dll");
                }
            }
            catch (Exception e)
            {
                AppendInLog(pathLogFile, e.Message);
                AppendInLog(pathLogFile, e.StackTrace);
            }

        }

        static void UpdateBySettings(UpdateSettings settings)
        {
            var updaterPath = Path.Combine(di.FullName, "updates", settings.DirectoryName);
            try
            {
                AppendInLog(pathLogFile, $"Start UpdateBySettings in directory {updaterPath}");
                AppendInLog(pathLogFile, $"Start CleanFiles");
                _CleanFiles(settings);
                AppendInLog(pathLogFile, $"Finish CleanFiles");
            }
            catch (Exception ex)
            {
                AppendInLog(pathLogFile, ex.StackTrace);
            }

            try
            {
                //Now Create all of the directories
                AppendInLog(pathLogFile, $"Start Create directories don't exist");
                var alldirs = Directory.GetDirectories(updaterPath, "*", SearchOption.AllDirectories);
                foreach (string dirPath in alldirs)
                {
                    var directoryName = dirPath.Replace(updaterPath, di.FullName);
                    try
                    {
                        AppendInLog(pathLogFile, $"Start Create directory {directoryName}");
                        Directory.CreateDirectory(directoryName);
                        AppendInLog(pathLogFile, $"Finish Create directory {directoryName}");
                    }
                    catch (Exception ex1)
                    {
                        AppendInLog(pathLogFile, $"Cannot create directory {directoryName} {Environment.NewLine} {ex1.Message}");
                        AppendInLog(pathLogFile, ex1.StackTrace);
                    }
                }
                AppendInLog(pathLogFile, $"Finish Create directories");
            }
            catch (Exception ex1)
            {
                AppendInLog(pathLogFile, $"Cannot create directories {ex1.Message}");
                AppendInLog(pathLogFile, ex1.StackTrace);
            }

            AppendInLog(pathLogFile, $"Start Copy all the files & Replaces any files with the same name");
            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(updaterPath, "*.*", SearchOption.AllDirectories))
                try
                {
                    File.Copy(newPath, newPath.Replace(updaterPath, di.FullName), true);
                }
                catch (Exception ex1)
                {
                    AppendInLog(pathLogFile, $"Cannot copy file {newPath} {ex1.Message}");
                    AppendInLog(pathLogFile, ex1.StackTrace);
                }
            AppendInLog(pathLogFile, $"Finish Copy all the files & Replaces any files with the same name");


            var webconfig = Path.Combine(di.FullName, "web.config");
            _replaceNetCoreDllInWebConfig(webconfig, "EgritosGroup.AspNetCore.Updater.dll", settings.AppEntryDll);
        }

        private static void _CleanFiles(UpdateSettings settings)
        {
            var fileInfo = new FileInfo(typeof(Program).Assembly.Location);
            //fileInfo = new FileInfo(@"\\egs02\wwwroot\kliseis.demo.egritosgroup.gr\EgritosGroup.AspNetCore.Updater.dll"); gia test
            var appDi = fileInfo.Directory;
            //var updaterPath = UnzipDirPath;// Path.Combine(appDi.FullName, "updates", UnzipDirPath);
            var extensions = new string[] { ".dll", ".pdb" };
            var removeFiles = appDi.GetFiles()
                                 .Where(f => extensions.Contains(f.Extension.ToLower()))
                                 .ToArray();

            var keepFiles = new string[] {
                "EgritosGroup.AspNetCore.Updater.dll", "EgritosGroup.AspNetCore.Updater.pdb", "Updater.log",
                "EgritosGroup.AspNetCore.Updater.deps.json", "EgritosGroup.AspNetCore.Updater.runtimeconfig.json",
            };

            foreach (var file in removeFiles)
                if (!keepFiles.Contains(file.Name))
                    file.Delete();

            //string[] folders = { "clientSources", "refs", "runtimes", "Views", "wwwroot" };
            var keepFolders = new string[] { "updates", "logs" };
            if (settings.ExcludedDirectoriesFromClean != null)
                keepFolders = keepFolders.Concat(settings.ExcludedDirectoriesFromClean).Select(x => x.ToLower()).ToArray();

            foreach (var dir in appDi.GetDirectories())
                if (!keepFolders.Contains(dir.Name.ToLower()))
                    dir.Delete(true);
        }

        private static void _replaceNetCoreDllInWebConfig(string webconfig, string oldDll, string newDll)
        {
            string text = File.ReadAllText(webconfig);
            text = text.Replace(oldDll, newDll);
            File.WriteAllText(webconfig, text);
        }

        private static void AppendInLog(string logPath, string txt)
        {
            if (!File.Exists(logPath))
            {
                File.Create(logPath).Dispose();
                using (TextWriter tw = new StreamWriter(logPath))
                {
                    tw.WriteLine(txt + Environment.NewLine);
                    tw.Close();
                }

            }
            else if (File.Exists(logPath))
            {
                File.AppendAllText(logPath, txt + Environment.NewLine);
            }
        }


        private static bool FileExists(string rootpath, string filename)
        {
            if (File.Exists(Path.Combine(rootpath, filename)))
                return true;

            foreach (string subDir in Directory.GetDirectories(rootpath, "*", SearchOption.AllDirectories))
            {
                if (File.Exists(Path.Combine(subDir, filename)))
                    return true;
            }

            return false;
        }
    }
}
