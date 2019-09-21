using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreAutoUpdater
{
    public class AppUpdaterOptions
    {
        private string _primaryArchive;

        public AppUpdaterOptions()
        {
            SecondaryFiles = new List<UpdaterFileInfo>();
        }

        public string AppName { get; set; }
        public string AppEntryDll { get; set; }

        public string ContentRootPath { get; set; }

        public string ArchiveName
        {
            get
            {
                if (string.IsNullOrEmpty(_primaryArchive))
                    return $"{AppName}.zip";
                return _primaryArchive;
            }
            set => _primaryArchive = value;
        }

        public bool SkipUpdateSettingsFile { get; set; }

        public List<UpdaterFileInfo> SecondaryFiles { get; set; }
    }
}
